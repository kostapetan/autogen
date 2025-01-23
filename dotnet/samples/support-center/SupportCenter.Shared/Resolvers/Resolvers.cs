// Copyright (c) Microsoft Corporation. All rights reserved.
// Resolvers.cs

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace SupportCenter.Shared.Resolvers;

public class Resolvers
{
    public delegate Kernel KernelResolver(string agent);
    public delegate ISemanticTextMemory SemanticTextMemoryResolver(string agent);
}
