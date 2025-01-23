// Copyright (c) Microsoft Corporation. All rights reserved.
// AgentExtensions.cs

using Google.Protobuf;
using SupportCenter.Shared.Hubs;

namespace SupportCenter.Agents.Extensions;

public static class AgentExtensions
{
    public static (string id, string userId, string userMessage) GetAgentData(this IMessage item)
    {
        var eventType = item.GetType();

        // Get UserId
        var userIdProperty = eventType.GetProperty("UserId") ?? eventType.GetProperty("user_id");
        var userId = userIdProperty?.GetValue(item)?.ToString() ?? string.Empty;

        // Get Message
        var messageProperty = eventType.GetProperty("Message") ?? eventType.GetProperty("message");
        var userMessage = messageProperty?.GetValue(item)?.ToString() ?? string.Empty;

        // Get ConversationId if it exists
        var conversationIdProperty = eventType.GetProperty("ConversationId") ?? eventType.GetProperty("conversation_id");
        var conversationId = conversationIdProperty?.GetValue(item)?.ToString() ?? string.Empty;

        // Generate ID
        // TODO: move this to Agents proj.
        var conversationIdValue = SignalRConnectionsDB.GetConversationId(userId) ?? conversationId ?? string.Empty;
        var id = $"{userId}/{conversationIdValue}";

        return (id, userId, userMessage);
    }
}
