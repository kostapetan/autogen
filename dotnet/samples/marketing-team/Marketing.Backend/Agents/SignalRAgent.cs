// Copyright (c) Microsoft Corporation. All rights reserved.
// SignalRAgent.cs

using Marketing.Shared;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;
using Microsoft.AutoGen.Contracts;
using Marketing.Backend.Hubs;

namespace Marketing.Backend.Agents;

[TopicSubscription("default")]
public class SignalRAgent([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat,ISignalRService signalRClient, ILogger<SignalRAgent> logger)
    : AiAgent<AgentState>(typeRegistry, chat, logger),
    IHandle<ArticleCreated>,
    IHandle<GraphicDesignCreated>,
    IHandle<SocialMediaPostCreated>,
    IHandle<AuditorAlert>
{
    public async Task Handle(SocialMediaPostCreated item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        await signalRClient.SendMessageToSpecificClient(item.UserId, item.SocialMediaPost, Hubs.AgentTypes.CommunityManager);
    }

    public async Task Handle(ArticleCreated item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        await signalRClient.SendMessageToSpecificClient(item.UserId, item.Article, Hubs.AgentTypes.Chat);
    }

    public async Task Handle(GraphicDesignCreated item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        await signalRClient.SendMessageToSpecificClient(item.UserId, item.ImageUri, Hubs.AgentTypes.GraphicDesigner);
    }

    public async Task Handle(AuditorAlert item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        await signalRClient.SendMessageToSpecificClient(item.UserId, item.AuditorAlertMessage, Hubs.AgentTypes.Auditor);
    }
}
