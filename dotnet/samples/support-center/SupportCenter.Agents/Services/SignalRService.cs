// Copyright (c) Microsoft Corporation. All rights reserved.
// SignalRService.cs

using Microsoft.AspNetCore.SignalR;
using SupportCenter.Shared.Hubs;

namespace SupportCenter.Agents.Services;

public class SignalRService(IHubContext<SupportCenterHub> hubContext) : ISignalRService
{
    public async Task SendMessageToSpecificClient(string userId, string message, AgentTypes agentType)
    {
        var connectionId = SignalRConnectionsDB.GetConversationId(userId) ?? throw new Exception("ConnectionId not found");
        var frontEndMessage = new FrontEndMessage()
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = connectionId,
            UserId = userId,
            Message = message,
            Sender = agentType.ToString()
        };
        await hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", frontEndMessage).ConfigureAwait(false);
    }
}
