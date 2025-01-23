// Copyright (c) Microsoft Corporation. All rights reserved.
// FrontEndMessage.cs

namespace SupportCenter.Shared.Hubs;

public class FrontEndMessage
{
    public required string Id { get; set; }
    public required string ConversationId { get; set; }
    public required string UserId { get; set; }
    public required string Message { get; set; }
    public required string Sender { get; set; }
}
