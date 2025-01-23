// Copyright (c) Microsoft Corporation. All rights reserved.
// CosmosDbContainerOptions.cs

using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Shared.Options;

public class CosmosDbContainerOptions
{
    [Required]
    public string? DatabaseName { get; set; }

    [Required]
    public string? ContainerName { get; set; }

    [Required]
    public string? PartitionKey { get; set; }

    [Required]
    public string? EntityName { get; set; }
}
