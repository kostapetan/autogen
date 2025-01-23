// Copyright (c) Microsoft Corporation. All rights reserved.
// DispatcherAgentConfiguration.cs

using Microsoft.SemanticKernel;
using SupportCenter.Shared.Options;

namespace SupportCenter.Shared.AgentsConfigurationFactory;

internal sealed class DispatcherAgentConfiguration : IAgentConfiguration
{
    public void ConfigureOpenAI(OpenAIOptions options)
    {
    }

    public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
    {
    }
}
