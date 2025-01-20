// Copyright (c) Microsoft Corporation. All rights reserved.
// ArticleHub.cs

using Marketing.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AutoGen.Core;
using StackExchange.Redis;

namespace Marketing.Backend.Hubs;

public class ArticleHub(Client client, IConnectionMultiplexer connection) : Hub<IArticleHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var db = connection.GetDatabase();
        await db.KeyDeleteAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// This method is called when a new message from the client arrives.
    /// </summary>
    /// <param name="frontEndMessage"></param>
    /// <returns></returns>
    public async Task ProcessMessage(FrontEndMessage frontEndMessage)
    {
        ArgumentNullException.ThrowIfNull(frontEndMessage);
        ArgumentNullException.ThrowIfNull(client);

        var evt = new UserChatInput { UserId = frontEndMessage.UserId, UserMessage = frontEndMessage.Message };

        await client.PublishEventAsync(evt, topic: Consts.TopicName, key: evt.UserId);
    }

    public async Task ConnectToAgent(string userId)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(client);

        var db = connection.GetDatabase();
        await db.StringSetAsync(userId, Context.ConnectionId);
        
        var evt = new UserConnected { UserId = userId };
        await client.PublishEventAsync(evt, topic: Consts.TopicName, key: userId);
    }
}
