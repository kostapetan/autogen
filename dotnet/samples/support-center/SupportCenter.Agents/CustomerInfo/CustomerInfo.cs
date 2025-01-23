// Copyright (c) Microsoft Corporation. All rights reserved.
// CustomerInfo.cs

using global::SupportCenter.Shared;
using Microsoft.AutoGen.Agents;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using SupportCenter.Agents.Extensions;

namespace SupportCenter.Agents.CustomerInfo;
[TopicSubscription("default")]
public class CustomerInfo(IAgentWorker worker, Kernel kernel, ISemanticTextMemory memory, [FromKeyedServices("EventTypes")] EventTypes typeRegistry, ILogger<CustomerInfo> logger)
    : SKAiAgent<CustomerInfoState>(worker, memory, kernel, typeRegistry),
    IHandle<CustomerInfoRequest>,
    IHandle<UserNewConversation>
{
    public async Task Handle(CustomerInfoRequest item)
    {
        var (id, userId, message) = item.GetAgentData();

        logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(CustomerInfo), typeof(CustomerInfoRequest), item);

        var notif = new CustomerInfoNotification
        {
            UserId = userId,
            Message = "I'm working on the user's request..."
        };
        await PublishEventAsync(notif.ToCloudEvent(AgentId.ToString())).ConfigureAwait(false);

        // Get the customer info via the planners.
        var prompt = CustomerInfoPrompts.GetCustomerInfo
            .Replace("{{$userId}}", userId)
            .Replace("{{$userMessage}}", message)
            .Replace("{{$history}}", AppendChatHistory(message));

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        // FunctionCallingStepwisePlanner
        var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions()
        {
            MaxIterations = 10,
        });
        var result = await planner.ExecuteAsync(_kernel, prompt).ConfigureAwait(false);
        logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(CustomerInfo), typeof(CustomerInfoRequest), result.FinalAnswer);

        var response = new CustomerInfoResponse
        {
            UserId = userId,
            Message = result.FinalAnswer
        };
        await PublishEventAsync(response.ToCloudEvent(AgentId.ToString())).ConfigureAwait(false);
    }

    public async Task Handle(UserNewConversation item)
    {
        // The user started a new conversation.
        _state.History.Clear();
    }
}
