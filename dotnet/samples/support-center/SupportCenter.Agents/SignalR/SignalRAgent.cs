// Copyright (c) Microsoft Corporation. All rights reserved.
// SignalRAgent.cs

using Google.Protobuf.WellKnownTypes;
using Microsoft.AutoGen.Agents;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Agents.Services;
using SupportCenter.Shared;

namespace SupportCenter.Agents.SignalR;

[TopicSubscription("default")]

public class SignalRAgent(
    IAgentWorker worker,
    Kernel kernel,
    ISemanticTextMemory memory,
    [FromKeyedServices("EventTypes")] EventTypes typeRegistry,
    ISignalRService signalRClient) : SKAiAgent<Empty>(worker, memory, kernel, typeRegistry),
    IHandle<QnAResponse>
{
    public async Task Handle(QnAResponse item)
    {
        var userId = item.UserId;
        var message = item.Message;

        await signalRClient.SendMessageToSpecificClient(userId, message, AgentTypes.QnA).ConfigureAwait(false);
    }
}
