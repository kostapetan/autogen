// Copyright (c) Microsoft Corporation. All rights reserved.
// Writer.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Backend.Agents;

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
        var prompt = $"""
                    This is a multi agent app. You are a Marketing Campaign writer Agent.
                    If the request is not for you, answer with <NOTFORME>.
                    If the request is about writing or modifying an existing campaign, then you should write a campaign based on the user request.
                    Write up to three paragraphs to promote the product the user is asking for.
                    Below are a series of inputs from the user that you can use.
                    If the input talks about twitter or images, dismiss it and return <NOTFORME>
                    Input: {item.UserMessage}
                    """;
        var newArticle = await CallFunction(prompt);

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
        var prompt = $"""
                    This is a multi agent app. You are a Marketing Campaign writer Agent.
                    If the request is not for you, answer with <NOTFORME>.
                    If the request is about writing or modifying an existing campaign, then you should write a campaign based on the user request.
                    The campaign is not compliant with the company policy, and you need to adjust it. This is the message from the automatic auditor agent regarding what is wrong with the original campaign
                    ---
                    Input: {item.AuditorAlertMessage}
                    ---
                    Return only the new campaign text but adjusted to the auditor request
                    """;
        var newArticle = await CallFunction(prompt);
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
}
