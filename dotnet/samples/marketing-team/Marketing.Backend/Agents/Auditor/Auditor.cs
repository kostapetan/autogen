// Copyright (c) Microsoft Corporation. All rights reserved.
// Auditor.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Backend.Agents.Auditor;

[TopicSubscription(Consts.TopicName)]
public class Auditor([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<Auditor> logger)
: AiAgent<AuditorState>(typeRegistry, chat, logger),
IHandle<AuditText>
{
    public async Task Handle(AuditText item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"[{nameof(Auditor)}] Event {nameof(AuditText)}. Text: {{Text}}", item.Text);

        //var context = new KernelArguments { ["input"] = AppendChatHistory(item.Text) };
        var auditorAnswer = await CallFunction(AuditorPrompts.AuditText);
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
