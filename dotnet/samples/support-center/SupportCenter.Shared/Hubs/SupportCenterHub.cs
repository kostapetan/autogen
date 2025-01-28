// Copyright (c) Microsoft Corporation. All rights reserved.
// SupportCenterHub.cs

using Microsoft.AspNetCore.SignalR;
using Microsoft.AutoGen.Core;

namespace SupportCenter.Shared.Hubs;
public class SupportCenterHub(IAgentWorker client) : Hub<ISupportCenterHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        SignalRConnectionsDB.ConnectionByUser.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// This method is called when a new message from the client arrives.
    /// </summary>
    /// <param name="frontEndMessage"></param>
    /// <returns></returns>
    public async Task ProcessMessage(FrontEndMessage frontEndMessage)
    {
        ArgumentNullException.ThrowIfNull(frontEndMessage);
        ArgumentNullException.ThrowIfNull(client);

        var evt = new UserChatInput { UserId = frontEndMessage.UserId, Message = frontEndMessage.Message };

        await client.PublishEventAsync(evt.ToCloudEvent(key: frontEndMessage.UserId, topic: Constants.TopicName))
            .ConfigureAwait(false);
    }

    public async Task RestartConversation(string userId, string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId, nameof(conversationId));

        var oldConversationId = SignalRConnectionsDB.GetConversationId(userId);

        SignalRConnectionsDB.ConnectionByUser.AddOrUpdate(
            userId,
            key => new Connection(Context.ConnectionId, conversationId),
            (key, oldValue) => new Connection(connectionId: oldValue.Id, conversationId));

        var evt = new UserNewConversation { UserId = userId };
        await client.PublishEventAsync(evt.ToCloudEvent(key: userId, topic: Constants.TopicName)).ConfigureAwait(false);
    }

    public async Task ConnectToAgent(string userId, string conversationId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(client);

        SignalRConnectionsDB.ConnectionByUser.AddOrUpdate(
            userId, new Connection(Context.ConnectionId, conversationId),
            (key, oldValue) => new Connection(Context.ConnectionId, conversationId));

        // Notify the agents that a new user got connected.
        var evt = new UserConnected { UserId = userId };
        await client.PublishEventAsync(evt.ToCloudEvent(userId, topic: Constants.TopicName)).ConfigureAwait(false);
    }
}
