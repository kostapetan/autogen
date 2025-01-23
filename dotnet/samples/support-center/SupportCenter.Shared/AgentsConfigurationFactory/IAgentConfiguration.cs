// Copyright (c) Microsoft Corporation. All rights reserved.
// IAgentConfiguration.cs

using Microsoft.SemanticKernel;
using SupportCenter.Shared.Options;

namespace SupportCenter.Shared.AgentsConfigurationFactory;

public interface IAgentConfiguration
{
    void ConfigureOpenAI(OpenAIOptions options);
    void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider);
}
