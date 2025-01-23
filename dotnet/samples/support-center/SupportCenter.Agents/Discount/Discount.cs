// Copyright (c) Microsoft Corporation. All rights reserved.
// Discount.cs

using global::SupportCenter.Shared;
using Microsoft.AutoGen.Agents;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace SupportCenter.Agents.Discount;
[TopicSubscription("default")]
public class Discount(IAgentWorker worker, Kernel kernel, ISemanticTextMemory memory, [FromKeyedServices("EventTypes")] EventTypes typeRegistry, ILogger<Discount> logger)
    : SKAiAgent<DiscountState>(worker, memory, kernel, typeRegistry),
    IHandle<UserNewConversation>
{

    public async Task Handle(UserNewConversation item)
    {
        logger.LogInformation($"[{nameof(Discount)}] Event {nameof(UserNewConversation)}");
        // The user started a new conversation.
        _state.History.Clear();
    }
}
