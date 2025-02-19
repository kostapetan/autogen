// Copyright (c) Microsoft Corporation. All rights reserved.
// GrpcAgentWorker.cs

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Channels;
using Grpc.Core;
using Microsoft.AutoGen.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AutoGen.Core.Grpc;

public sealed class GrpcAgentWorker(
    AgentRpc.AgentRpcClient client,
    IHostApplicationLifetime hostApplicationLifetime,
    IServiceProvider serviceProvider,
    [FromKeyedServices("AgentTypes")] IEnumerable<Tuple<string, Type>> configuredAgentTypes,
    ILogger<GrpcAgentWorker> logger,
    DistributedContextPropagator distributedContextPropagator) :
    IHostedService, IDisposable, IAgentWorker
{
    private readonly object _channelLock = new();
    private readonly ConcurrentDictionary<string, Type> _agentTypes = new();
    private readonly ConcurrentDictionary<(string Type, string Key), Agent> _agents = new();
    private readonly ConcurrentDictionary<string, (Agent Agent, string OriginalRequestId)> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, HashSet<Type>> _agentsForEvent = new();
    private readonly Channel<(Message Message, TaskCompletionSource WriteCompletionSource)> _outboundMessagesChannel = Channel.CreateBounded<(Message, TaskCompletionSource)>(new BoundedChannelOptions(1024)
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly AgentRpc.AgentRpcClient _client = client;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly IEnumerable<Tuple<string, Type>> _configuredAgentTypes = configuredAgentTypes;
    private readonly ILogger<GrpcAgentWorker> _logger = logger;
    private readonly DistributedContextPropagator _distributedContextPropagator = distributedContextPropagator;
    private readonly CancellationTokenSource _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping);
    private readonly TaskCompletionSource _channelOpen = new();
    private AsyncDuplexStreamingCall<Message, Message>? _channel;
    private Task? _readTask;
    private Task? _writeTask;

    public void Dispose()
    {
        _outboundMessagesChannel.Writer.TryComplete();
        _channel?.Dispose();
    }
    private async Task RunReadPump()
    {
        var channel = await GetChannel();
        while (!_shutdownCts.Token.IsCancellationRequested)
        {
            try
            {
                await foreach (var message in channel.ResponseStream.ReadAllAsync(_shutdownCts.Token))
                {
                    // next if message is null
                    if (message == null)
                    {
                        continue;
                    }
                    switch (message.MessageCase)
                    {
                        case Message.MessageOneofCase.Request:
                            GetOrActivateAgent(message.Request.Target).ReceiveMessage(message);
                            break;
                        case Message.MessageOneofCase.Response:
                            if (!_pendingRequests.TryRemove(message.Response.RequestId, out var request))
                            {
                                throw new InvalidOperationException($"Unexpected response '{message.Response}'");
                            }

                            message.Response.RequestId = request.OriginalRequestId;
                            request.Agent.ReceiveMessage(message);
                            break;

                        case Message.MessageOneofCase.CloudEvent:

                            var item = message.CloudEvent;
                            if (!_agentsForEvent.TryGetValue(item.Type, out var agents))
                            {
                                throw new InvalidOperationException($"This worker can't handle the event type '{item.Type}'.");
                            }
                            foreach (var a in agents)
                            {
                                var agent = GetOrActivateAgent(new AgentId { Type = a.Name, Key = item.GetSubject() });
                                agent.ReceiveMessage(message);
                            }

                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected message '{message}'.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Time to shut down.
                break;
            }
            catch(RpcException exception) when (exception.Status.Detail =="stream timeout")
            {
                _logger.LogError(exception, "Reset rpc stream");
                channel = RecreateChannel(channel);
            }
            catch (Exception ex) when (!_shutdownCts.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error reading from channel.");
                channel = RecreateChannel(channel);
            }
            catch
            {
                // Shutdown requested.
                break;
            }
        }
    }
    private async Task RunWritePump()
    {
        var channel = await GetChannel();
        var outboundMessages = _outboundMessagesChannel.Reader;
        while (!_shutdownCts.IsCancellationRequested)
        {
            (Message Message, TaskCompletionSource WriteCompletionSource) item = default;
            try
            {
                await outboundMessages.WaitToReadAsync().ConfigureAwait(false);

                // Read the next message if we don't already have an unsent message
                // waiting to be sent.
                if (!outboundMessages.TryRead(out item))
                {
                    break;
                }

                while (!_shutdownCts.IsCancellationRequested)
                {
                    await channel.RequestStream.WriteAsync(item.Message, _shutdownCts.Token).ConfigureAwait(false);
                    item.WriteCompletionSource.TrySetResult();
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // Time to shut down.
                item.WriteCompletionSource?.TrySetCanceled();
                break;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                // we could not connect to the endpoint - most likely we have the wrong port or failed ssl
                // we need to let the user know what port we tried to connect to and then do backoff and retry
                _logger.LogError(ex, "Error connecting to GRPC endpoint {Endpoint}.", channel.ToString());
                break;
            }
            catch (Exception ex) when (!_shutdownCts.IsCancellationRequested)
            {
                item.WriteCompletionSource?.TrySetException(ex);
                _logger.LogError(ex, "Error writing to channel.");
                channel = RecreateChannel(channel);
                continue;
            }
            catch
            {
                // Shutdown requested.
                item.WriteCompletionSource?.TrySetCanceled();
                break;
            }
        }

        while (outboundMessages.TryRead(out var item))
        {
            item.WriteCompletionSource.TrySetCanceled();
        }
    }

    private Agent GetOrActivateAgent(AgentId agentId)
    {
        if (!_agents.TryGetValue((agentId.Type, agentId.Key), out var agent))
        {
            if (_agentTypes.TryGetValue(agentId.Type, out var agentType))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedProvider = scope.ServiceProvider;
                    var context = new RuntimeContext(agentId, this, scopedProvider.GetRequiredService<ILogger<Agent>>(), _distributedContextPropagator);
                    agent = (Agent)ActivatorUtilities.CreateInstance(scopedProvider, agentType);
                    Agent.Initialize(context, agent);
                    _agents.TryAdd((agentId.Type, agentId.Key), agent);
                }
            }
            else
            {
                throw new InvalidOperationException($"Agent type '{agentId.Type}' is unknown.");
            }
        }

        return agent;
    }

    private async ValueTask RegisterAgentTypeAsync(string type, Type agentType, CancellationToken cancellationToken = default)
    {
        if (_agentTypes.TryAdd(type, agentType))
        {
            // get the events that the agent handles
            var events = agentType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandle<>))
            .Select(i => ReflectionHelper.GetMessageDescriptor(i.GetGenericArguments().First())?.FullName);
            //var state = agentType.BaseType?.GetGenericArguments().First();
            // add the agentType to the list of agent types that handle the event
            foreach (var evt in events)
            {
                if (!_agentsForEvent.TryGetValue(evt!, out var agents))
                {
                    agents = new HashSet<Type>();
                    _agentsForEvent[evt!] = agents;
                }

                agents.Add(agentType);
            }

            var topicTypes = agentType.GetCustomAttributes<TopicSubscriptionAttribute>().Select(t => t.Topic);

            _logger.LogInformation($"{cancellationToken.ToString}"); // TODO: remove this
            var response = await _client.RegisterAgentAsync(new RegisterAgentTypeRequest
            {
                Type = type,
                Topics = { topicTypes },
                //StateType = state?.Name,
                Events = { events }
            }, null, null, cancellationToken);
        }
    }
    // new is intentional
    public async ValueTask SendResponseAsync(RpcResponse response, CancellationToken cancellationToken = default)
    {
        await WriteChannelAsync(new Message { Response = response }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SendRequestAsync(Agent agent, RpcRequest request, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        _pendingRequests[requestId] = (agent, request.RequestId);
        request.RequestId = requestId;
        await WriteChannelAsync(new Message { Request = request }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SendMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await WriteChannelAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask PublishEventAsync(CloudEvent @event, CancellationToken cancellationToken = default)
    {
        await WriteChannelAsync(new Message { CloudEvent = @event }, cancellationToken).ConfigureAwait(false);
    }
    private async Task WriteChannelAsync(Message message, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        await _outboundMessagesChannel.Writer.WriteAsync((message, tcs), cancellationToken).ConfigureAwait(false);
    }
    private async Task<AsyncDuplexStreamingCall<Message, Message>> GetChannel()
    {
        if (_channel is { } channel)
        {
            return channel;
        }
        AsyncDuplexStreamingCall<Message, Message> chn;
        lock (_channelLock)
        {
            if (_channel is not null)
            {
                return _channel;
            }

            chn = RecreateChannel(null);
        }

        await RegisterAgents(CancellationToken.None).ConfigureAwait(false);
        return chn;
    }

    private AsyncDuplexStreamingCall<Message, Message> RecreateChannel(AsyncDuplexStreamingCall<Message, Message>? channel)
    {
        if (_channel is null || _channel == channel)
        {
            lock (_channelLock)
            {
                if (_channel is null || _channel == channel)
                {
                    _channel?.Dispose();
                    _channel = _client.OpenChannel(cancellationToken: _shutdownCts.Token);
                    // TODO: When we recreate the channel, we need to send the metadata again (via register agent)
                    _channelOpen.TrySetResult();
                }
            }
        }
        _logger.LogInformation("Channel is open");
        return _channel;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await GetChannel();
        StartCore();
       
        async void StartCore()
        {
            var didSuppress = false;
            if (!ExecutionContext.IsFlowSuppressed())
            {
                didSuppress = true;
                ExecutionContext.SuppressFlow();
            }

            try
            {
                _readTask = Task.Run(RunReadPump, CancellationToken.None);
                _writeTask = Task.Run(RunWritePump, CancellationToken.None);
                await RegisterAgents(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                if (didSuppress && ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }
    }

    public async ValueTask RegisterAgents(CancellationToken cancellationToken)
    {
        await _channelOpen.Task;
        foreach (var (typeName, type) in _configuredAgentTypes)
        {
            await RegisterAgentTypeAsync(typeName, type, cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCts.Cancel();

        _outboundMessagesChannel.Writer.TryComplete();

        if (_readTask is { } readTask)
        {
            await readTask.ConfigureAwait(false);
        }

        if (_writeTask is { } writeTask)
        {
            await writeTask.ConfigureAwait(false);
        }
        lock (_channelLock)
        {
            _channel?.Dispose();
        }
    }

    public async ValueTask StoreAsync(AgentState value, CancellationToken cancellationToken = default)
    {
        var agentId = value.AgentId ?? throw new InvalidOperationException("AgentId is required when saving AgentState.");
        var response = _client.SaveState(value, null, null, cancellationToken);
        if (!response.Success)
        {
            throw new InvalidOperationException($"Error saving AgentState for AgentId {agentId}.");
        }
    }

    public async ValueTask<AgentState> ReadAsync(AgentId agentId, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetStateAsync(agentId).ConfigureAwait(true);
        return response.AgentState;
    }
}

