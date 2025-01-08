// Copyright (c) Microsoft Corporation. All rights reserved.
// Writer.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Agents;

[TopicSubscription("default")]
public class Writer([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<Writer> logger)
    : AiAgent<CommunityManagerState>(typeRegistry, chat, logger),
    IHandle<UserConnected>,
    IHandle<UserChatInput>,
    IHandle<AuditorAlert>
{
    public async Task Handle(UserConnected item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"User Connected: {item.UserId}");
        string? lastMessage = "";// _state.History.LastOrDefault()?.Message;
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
        // TODO: Implement
       // var agentState = _state.Data.ToAgentState(this.AgentId, "Etag");
      //  await Store("WrittenArticle", newArticle);
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

        await PublishEventAsync(articleCreatedEvent);
        await PublishEventAsync(auditTextEvent);
    }

    public Task<string> GetArticle()
    {
        //return Task.FromResult(_state.Data.WrittenArticle);
        return Task.FromResult("");
    }
}
