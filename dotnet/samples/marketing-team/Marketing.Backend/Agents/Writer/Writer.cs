// Copyright (c) Microsoft Corporation. All rights reserved.
// Writer.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Backend.Agents.Writer;

[TopicSubscription(Consts.TopicName)]
public class Writer([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<Writer> logger)
    : AiAgent<CommunityManagerState>(typeRegistry, chat, logger),
    IHandle<UserConnected>,
    IHandle<UserChatInput>,
    IHandle<AuditorAlert>
{
    public async Task Handle(UserConnected item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"User Connected: {item.UserId}");
        var lastMessage = "";// _state.History.LastOrDefault()?.Message;
        if (string.IsNullOrWhiteSpace(lastMessage))
        {
            return;
        }

        await SendArticleCreatedEvent(lastMessage, item.UserId);
    }

    public async Task Handle(UserChatInput item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"UserChatInput: {item.UserMessage}");
        //var context = new KernelArguments { ["input"] = AppendChatHistory(item.UserMessage) };
        var newArticle = await CallFunction(WriterPrompts.Write);

        if (newArticle.Contains("NOTFORME", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        var agentState = await ReadAsync<CommunityManagerState>(AgentId);
        agentState.Article = newArticle;
        await StoreAsync(agentState.ToAgentState(AgentId, ""));
        await SendArticleCreatedEvent(newArticle, item.UserId);
    }

    public async Task Handle(AuditorAlert item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Auditor feedback: {item.AuditorAlertMessage}");

        var newArticle = await CallFunction(WriterPrompts.Adjust);

        if (newArticle.Contains("NOTFORME", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }
        await SendArticleCreatedEvent(newArticle, item.UserId);
    }
    private async Task SendArticleCreatedEvent(string article, string userId)
    {
        var articleCreatedEvent = new ArticleCreated
        {
            Article = article,
            UserId = userId
        };

        var auditTextEvent = new AuditText
        {
            Text = "Article written by the Writer: " + article,
            UserId = userId
        };

        await PublishEventAsync(articleCreatedEvent, topic: Consts.TopicName);
        await PublishEventAsync(auditTextEvent, topic: Consts.TopicName);
    }

    public Task<string> GetArticle()
    {
        //return Task.FromResult(_state.Data.WrittenArticle);
        return Task.FromResult("");
    }
}
