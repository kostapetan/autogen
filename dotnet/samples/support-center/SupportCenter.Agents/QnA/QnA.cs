// Copyright (c) Microsoft Corporation. All rights reserved.
// QnA.cs

using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Shared;
using SupportCenter.Shared.SemanticKernel;

namespace SupportCenter.Agents.QnA;

[TopicSubscription(Constants.TopicName)]
public class QnA(
    [FromKeyedServices("AgentsMetadata")] AgentsMetadata agentsMetadata,
    ISemanticTextMemory memory,
    Kernel kernel,
    ILogger<QnA> logger) : SKAiAgent<QnAState>(agentsMetadata, memory, kernel, logger),
    IHandle<QnARequest>
{
    public async Task Handle(QnARequest item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"[{nameof(QnA)}] Event {nameof(QnARequest)}. Text: {{Text}}", item.Message);

        var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
        var answer = await CallFunction(QnAPrompts.QnAGenericPrompt, context);
        if (answer.Contains("NOTFORME", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        await SendQnAResponse(answer, item.UserId).ConfigureAwait(false);
    }

    private async Task SendQnAResponse(string message, string userId)
    {
        var qna = new QnAResponse
        {
            Message = message,
            UserId = userId
        };

        await PublishEventAsync(@event: qna, topic: Constants.TopicName).ConfigureAwait(false);
    }
}
