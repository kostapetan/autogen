// Copyright (c) Microsoft Corporation. All rights reserved.
// Customer.cs

using System.Text.Json.Serialization;
using SupportCenter.Shared.Data.Entities;

namespace SupportCenter.Shared.Data.CosmosDb.Entities;

public class Customer : Entity
{
    [JsonPropertyName(nameof(Name))]
    public string? Name { get; set; }

    [JsonPropertyName(nameof(Email))]
    public string? Email { get; set; }

    [JsonPropertyName(nameof(Phone))]
    public string? Phone { get; set; }

    [JsonPropertyName(nameof(Address))]
    public string? Address { get; set; }

    public override string GetPartitionKeyValue() => Id;
}
