// Copyright (c) Microsoft Corporation. All rights reserved.
// OpenAIOptions.cs

using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Shared.Options;

public class OpenAIOptions
{
    // Embeddings
    [Required]
    public string EmbeddingsEndpoint { get; set; } = string.Empty;
    [Required]
    public string EmbeddingsApiKey { get; set; } = string.Empty;
    [Required]
    public string EmbeddingsDeploymentOrModelId { get; set; } = string.Empty;

    // Chat
    [Required]
    public string ChatEndpoint { get; set; } = string.Empty;
    [Required]
    public string ChatApiKey { get; set; } = string.Empty;
    [Required]
    public string ChatDeploymentOrModelId { get; set; } = string.Empty;

    public string? InvoiceDeploymentOrModelId { get; set; }
    public string? ConversationDeploymentOrModelId { get; set; }
}
