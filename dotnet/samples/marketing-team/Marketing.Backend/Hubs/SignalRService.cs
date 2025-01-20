// Copyright (c) Microsoft Corporation. All rights reserved.
// SignalRService.cs

using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Marketing.Backend.Hubs;

public class SignalRService(IHubContext<ArticleHub> hubContext, IConnectionMultiplexer connectionMux) : ISignalRService
{
    public async Task SendMessageToSpecificClient(string userId, string message, AgentTypes agentType)
    {
        var db = connectionMux.GetDatabase();
        var connectionId = await db.StringGetAsync(userId);
        var frontEndMessage = new FrontEndMessage()
        {
            UserId = userId,
            Message = message,
            Agent = agentType.ToString()
        };
        await hubContext.Clients.Client(connectionId!).SendAsync("ReceiveMessage", frontEndMessage);
    }
}
