// Copyright (c) Microsoft Corporation. All rights reserved.
// Invoice.cs

using global::SupportCenter.Shared;
using Microsoft.AutoGen.Agents;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace SupportCenter.Agents.Invoice;
[TopicSubscription("default")]
public class Invoice(IAgentWorker worker, Kernel kernel, ISemanticTextMemory memory, [FromKeyedServices("EventTypes")] EventTypes typeRegistry, ILogger<Invoice> logger)
    : SKAiAgent<InvoiceState>(worker, memory, kernel, typeRegistry),
    IHandle<InvoiceRequest>
{
    public async Task Handle(InvoiceRequest item)
    {
        var userId = item.UserId;
        var message = item.Message;

        logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Invoice), typeof(InvoiceRequest), item);

        var notif = new InvoiceNotification
        {
            UserId = userId,
            Message = "Please wait while I look up the details for invoice..."
        };
        await PublishEventAsync(notif.ToCloudEvent(AgentId.ToString())).ConfigureAwait(false);

        var querycontext = new KernelArguments { ["input"] = AppendChatHistory(message) };
        var instruction = "Consider the following knowledge:!invoices!";
        var enhancedContext = await AddKnowledge(instruction, "invoices", querycontext).ConfigureAwait(false);
        var answer = await CallFunction(InvoicePrompts.InvoiceRequest, enhancedContext).ConfigureAwait(false);

        var response = new InvoiceResponse
        {
            UserId = userId,
            Message = answer
        };
        await PublishEventAsync(response.ToCloudEvent(AgentId.ToString())).ConfigureAwait(false);
    }
}
