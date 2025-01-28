// Copyright (c) Microsoft Corporation. All rights reserved.
// SignalRAgent.cs

using Google.Protobuf.WellKnownTypes;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Agents.Services;
using SupportCenter.Shared;
using SupportCenter.Shared.SemanticKernel;

namespace SupportCenter.Agents.SignalR;

public class SignalRAgent(
    [FromKeyedServices("AgentsMetadata")] AgentsMetadata agentsMetadata,
    ISemanticTextMemory memory,
    Kernel kernel,
    ILogger<SignalRAgent> logger,
    ISignalRService signalRClient)
    : SKAiAgent<Empty>(agentsMetadata, memory, kernel, logger),
    IHandle<QnAResponse>
{
    public async Task Handle(QnAResponse item, CancellationToken cancellationToken)
    {
        var userId = item.UserId;
        var message = item.Message;

        await signalRClient.SendMessageToSpecificClient(userId, message, AgentTypes.QnA).ConfigureAwait(false);
    }
}
