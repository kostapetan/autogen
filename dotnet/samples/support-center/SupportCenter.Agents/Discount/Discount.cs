// Copyright (c) Microsoft Corporation. All rights reserved.
// Discount.cs

using global::SupportCenter.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Shared.SemanticKernel;

namespace SupportCenter.Agents.Discount;
[TopicSubscription(Constants.TopicName)]
public class Discount(
   [FromKeyedServices("AgentsMetadata")] AgentsMetadata agentsMetadata,
    ISemanticTextMemory memory,
    Kernel kernel,
    ILogger<Discount> logger)
    : SKAiAgent<CustomerInfoState>(agentsMetadata, memory, kernel, logger),
    IHandle<UserNewConversation>
{

    public async Task Handle(UserNewConversation item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"[{nameof(Discount)}] Event {nameof(UserNewConversation)}");
        // The user started a new conversation.
        _state.History.Clear();
    }
}
