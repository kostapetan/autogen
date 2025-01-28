// Copyright (c) Microsoft Corporation. All rights reserved.
// CustomerInfo.cs

using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using SupportCenter.Agents.Extensions;
using SupportCenter.Shared;
using SupportCenter.Shared.SemanticKernel;

namespace SupportCenter.Agents.CustomerInfo;
[TopicSubscription(Constants.TopicName)]
public class CustomerInfo(
    [FromKeyedServices("AgentsMetadata")] AgentsMetadata agentsMetadata,
    ISemanticTextMemory memory,
    Kernel kernel,
    ILogger<CustomerInfo> logger)
    : SKAiAgent<CustomerInfoState>(agentsMetadata, memory, kernel, logger),
    IHandle<CustomerInfoRequest>,
    IHandle<UserNewConversation>
{
    public async Task Handle(CustomerInfoRequest item, CancellationToken cancellationToken)
    {
        var (id, userId, message) = item.GetAgentData();

        logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(CustomerInfo), typeof(CustomerInfoRequest), item);

        var notification = new CustomerInfoNotification
        {
            UserId = userId,
            Message = "I'm working on the user's request..."
        };
        await PublishEventAsync(@event: notification, topic: Constants.TopicName).ConfigureAwait(false);

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
        await PublishEventAsync(@event: response, topic: Constants.TopicName).ConfigureAwait(false);
    }

    public async Task Handle(UserNewConversation item, CancellationToken cancellationToken)
    {
        // The user started a new conversation.
        _state.History.Clear();
    }
}
