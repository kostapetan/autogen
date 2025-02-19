// Copyright (c) Microsoft Corporation. All rights reserved.
// CommunityManager.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Backend.Agents;

[TopicSubscription(Consts.TopicName)]
public class CommunityManager([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<CommunityManager> logger)
    : AiAgent<CommunityManagerState>(typeRegistry, chat, logger),
    IHandle<UserConnected>,
    IHandle<UserChatInput>,
    IHandle<ArticleCreated>
{
    public async Task Handle(ArticleCreated item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Article created: {item.Article}");
        //_state.Data.Article = item.Article;
        await HandleGeneration(item.UserId, item.UserMessage);
    }

    public async Task Handle(UserChatInput item, CancellationToken cancellationToken)
    {
        logger.LogInformation($"UserChatInput: {item.UserMessage}");
        // if (_state.Data.Article == null) { return; }
        await HandleGeneration(item.UserId, item.UserMessage);
    }

    private async Task HandleGeneration(string userId, string userMessage)
    {
        var input = /*_state.Data.Article +*/ "| USER REQUEST: " + userMessage;
        var prompt = $"""
                    You are a Marketing community manager writer.
                    If the request from the user is to write a post to promote a new product, or if it is specifically talking to you (community manager) 
                    then you should write a post based on the user request.
                    Your writings are going to be posted on Twitter. So be informal, friendly and add some hashtags and emojis.
                    Remember, the tweet cannot be longer than 280 characters.
                    If the request was not intended for you, reply with <NOTFORME>"
                    ---
                    Input: {input}
                    ---
                    """;
        var socialMediaPost = await CallFunction(prompt);
        if (socialMediaPost.Contains("NOTFORME", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }
        //_state.Data.WrittenSocialMediaPost = socialMediaPost;

        await SendDesignedCreatedEvent(socialMediaPost, userId);
    }

    public async Task Handle(UserConnected item, CancellationToken cancellationToken)
    {
        //The user reconnected, let's send the last message if we have one
        var lastMessage = "";// _state.History.LastOrDefault()?.Message;
        if (string.IsNullOrWhiteSpace(lastMessage))
        {
            return;
        }

        await SendDesignedCreatedEvent(lastMessage, item.UserId);
    }

    private async Task SendDesignedCreatedEvent(string socialMediaPost, string userId)
    {
        var socialMediaPostCreatedEvent = new SocialMediaPostCreated
        {
            SocialMediaPost = socialMediaPost,
            UserId = userId
        };

        await PublishEventAsync(socialMediaPostCreatedEvent, topic: Consts.TopicName);
    }

    // This is just an example on how you can synchronously call an specific agent
    public Task<string> GetArticle()
    {
        //return Task.FromResult(_state.Data.WrittenSocialMediaPost);
        return Task.FromResult("");
    }
}
