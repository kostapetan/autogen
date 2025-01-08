// Copyright (c) Microsoft Corporation. All rights reserved.
// GraphicDesigner.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace Marketing.Backend.Agents.GraphicDesigner;

[TopicSubscription("default")]
public class GraphicDesigner([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<GraphicDesigner> logger)
    : AiAgent<GraphicDesignerState>(typeRegistry, chat, logger),
    IHandle<UserConnected>,
    IHandle<ArticleCreated>
{
    public async Task Handle(UserConnected item, CancellationToken cancellationToken)
    {
        var lastMessage = "";// _state.History.LastOrDefault()?.Message;
        if (string.IsNullOrWhiteSpace(lastMessage))
        {
            return;
        }

        await SendDesignedCreatedEvent(lastMessage, item.UserId);
    }

    public async Task Handle(ArticleCreated item, CancellationToken cancellationToken)
    {
        //For demo purposes, we do not recreate images if they already exist
        //if (!string.IsNullOrEmpty(_state.Data.ImageUrl))
        //{
        //    return;
        //}

        logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(ArticleCreated)}.");
        //var dallEService = _kernel.GetRequiredService<ITextToImageService>();
        var imageUri = "";// await dallEService.GenerateImageAsync(item.Article, 1024, 1024);

        //_state.Data.ImageUrl = imageUri;

        await SendDesignedCreatedEvent(imageUri, item.UserId);
    }

    private async Task SendDesignedCreatedEvent(string imageUri, string userId)
    {
        var graphicDesignEvent = new GraphicDesignCreated
        {
            ImageUri = imageUri,
            UserId = userId
        };

        await PublishEventAsync(graphicDesignEvent);
    }
}
