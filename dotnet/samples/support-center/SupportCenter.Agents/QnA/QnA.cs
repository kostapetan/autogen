// Copyright (c) Microsoft Corporation. All rights reserved.
// QnA.cs

using Microsoft.AutoGen.Agents;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Shared;

namespace SupportCenter.Agents.QnA;

[TopicSubscription("default")]
public class QnA(IAgentWorker worker, Kernel kernel, ISemanticTextMemory memory, [FromKeyedServices("EventTypes")] EventTypes typeRegistry, ILogger<QnA> logger)
    : SKAiAgent<QnAState>(worker, memory, kernel, typeRegistry),
    IHandle<QnARequest>
{
    public async Task Handle(QnARequest item)
    {
        logger.LogInformation($"[{nameof(QnA)}] Event {nameof(QnARequest)}. Text: {{Text}}", item.Message);

        var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
        var answer = await CallFunction(QnAPrompts.QnAGenericPrompt, context);
        if (answer.Contains("NOTFORME", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        await SendQnAResponse(answer, item.UserId);
    }

    private async Task SendQnAResponse(string message, string userId)
    {
        var qnaresponse = new QnAResponse
        {
            Message = message,
            UserId = userId
        }.ToCloudEvent(AgentId.Key);

        await PublishEventAsync(qnaresponse);
    }
}
