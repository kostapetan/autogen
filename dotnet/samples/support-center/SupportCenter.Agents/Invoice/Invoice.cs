// Copyright (c) Microsoft Corporation. All rights reserved.
// Invoice.cs

using global::SupportCenter.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Shared.SemanticKernel;

namespace SupportCenter.Agents.Invoice;
[TopicSubscription(Constants.TopicName)]
public class Invoice(
    [FromKeyedServices("AgentsMetadata")] AgentsMetadata agentsMetadata,
    ISemanticTextMemory memory,
    Kernel kernel,
    ILogger<Invoice> logger)
    : SKAiAgent<InvoiceState>(agentsMetadata, memory, kernel, logger),
    IHandle<InvoiceRequest>
{
    public async Task Handle(InvoiceRequest item, CancellationToken cancellationToken)
    {
        var userId = item.UserId;
        var message = item.Message;

        logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Invoice), typeof(InvoiceRequest), item);

        var notification = new InvoiceNotification
        {
            UserId = userId,
            Message = "Please wait while I look up the details for invoice..."
        };
        await PublishEventAsync(@event: notification, topic: Constants.TopicName).ConfigureAwait(false);

        var querycontext = new KernelArguments { ["input"] = AppendChatHistory(message) };
        var instruction = "Consider the following knowledge:!invoices!";
        var enhancedContext = await AddKnowledge(instruction, "invoices", querycontext).ConfigureAwait(false);
        var answer = await CallFunction(InvoicePrompts.InvoiceRequest, enhancedContext).ConfigureAwait(false);

        var response = new InvoiceResponse
        {
            UserId = userId,
            Message = answer
        };
        await PublishEventAsync(@event: response, topic: Constants.TopicName).ConfigureAwait(false);
    }
}
