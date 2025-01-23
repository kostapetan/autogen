// Copyright (c) Microsoft Corporation. All rights reserved.
// CustomerInfoAgentConfiguration.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SupportCenter.Shared.Options;
using SupportCenter.Shared.SemanticKernel.Plugins.CustomerPlugin;

namespace SupportCenter.Shared.AgentsConfigurationFactory;

public class CustomerInfoAgentConfiguration : IAgentConfiguration
{
    private readonly string customerPlugin = "CustomerPlugin";

    public void ConfigureOpenAI(OpenAIOptions options)
    {
        options.ChatDeploymentOrModelId = options.ConversationDeploymentOrModelId ?? options.ChatDeploymentOrModelId;
    }

    public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
    {
        if (kernel.Plugins.TryGetPlugin(customerPlugin, out _) == false)
        {
            kernel.ImportPluginFromObject(serviceProvider.GetRequiredService<CustomerData>(), customerPlugin);
        }
    }
}
