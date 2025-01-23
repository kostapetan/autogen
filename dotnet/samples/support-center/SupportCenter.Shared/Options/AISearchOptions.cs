// Copyright (c) Microsoft Corporation. All rights reserved.
// AISearchOptions.cs

using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Shared.Options;
public class AISearchOptions
{
    // AI Search
    [Required]
    public string? SearchEndpoint { get; set; }
    [Required]
    public string? SearchKey { get; set; }
    [Required]
    public string? SearchIndex { get; set; }
    // Embeddings
    [Required]
    public string? SearchEmbeddingDeploymentOrModelId { get; set; }
    [Required]
    public string? SearchEmbeddingEndpoint { get; set; }
    [Required]
    public string? SearchEmbeddingApiKey { get; set; }
}
