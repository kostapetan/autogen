// Copyright (c) Microsoft Corporation. All rights reserved.
// QdrantOptions.cs

using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Shared.Options;
public class QdrantOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    [Required]
    public int VectorSize { get; set; }
}
