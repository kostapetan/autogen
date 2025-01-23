// Copyright (c) Microsoft Corporation. All rights reserved.
// InvoiceAgentConfiguration.cs

using Microsoft.SemanticKernel;
using SupportCenter.Shared.Options;

namespace SupportCenter.Shared.AgentsConfigurationFactory;

public class InvoiceAgentConfiguration : IAgentConfiguration
{
    public void ConfigureOpenAI(OpenAIOptions options)
    {
        options.ChatDeploymentOrModelId = options.InvoiceDeploymentOrModelId ?? options.ChatDeploymentOrModelId;
    }

    public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
    {
    }
}
