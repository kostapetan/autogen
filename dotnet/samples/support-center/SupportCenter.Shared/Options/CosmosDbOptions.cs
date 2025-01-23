// Copyright (c) Microsoft Corporation. All rights reserved.
// CosmosDbOptions.cs

namespace SupportCenter.Shared.Options;

public class CosmosDbOptions
{
    public string? AccountUri { get; set; }

    public string? AccountKey { get; set; }

    public IEnumerable<CosmosDbContainerOptions>? Containers { get; set; }
}
