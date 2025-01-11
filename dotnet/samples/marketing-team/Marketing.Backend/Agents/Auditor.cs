// Copyright (c) Microsoft Corporation. All rights reserved.
// Auditor.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Backend.Agents;

[TopicSubscription(Consts.TopicName)]
public class Auditor([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<Auditor> logger)
: AiAgent<AuditorState>(typeRegistry, chat, logger),
IHandle<AuditText>
{
    public async Task Handle(AuditText item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"[{nameof(Auditor)}] Event {nameof(AuditText)}. Text: {{Text}}", item.Text);

        var prompt = $"""
                    You are an Auditor in a Marketing team.
                    Audit the text below and make sure we do not give discounts larger than 50%.
                    If the text talks about a larger than 50% discount, reply with a message to the user saying that the discount is too large, and by company policy we are not allowed.
                    If the message says who wrote it, add that information in the response as well.
                    In any other case, reply with NOTFORME
                    ---
                    Input: {item.Text}
                    ---
                    """;
        var auditorAnswer = await CallFunction(prompt);
        if (auditorAnswer.Contains("NOTFORME", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        await SendAuditorAlertEvent(auditorAnswer, item.UserId);
    }

    private async Task SendAuditorAlertEvent(string auditorAlertMessage, string userId)
    {
        var auditorAlert = new AuditorAlert
        {
            AuditorAlertMessage = auditorAlertMessage,
            UserId = userId
        };

        await PublishEventAsync(auditorAlert, topic: Consts.TopicName);
    }
}
