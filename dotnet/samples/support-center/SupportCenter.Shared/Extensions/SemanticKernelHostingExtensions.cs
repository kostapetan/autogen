// Copyright (c) Microsoft Corporation. All rights reserved.
// SemanticKernelHostingExtensions.cs

#pragma warning disable SKEXP0050
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;
using SupportCenter.Shared.AgentsConfigurationFactory;
using SupportCenter.Shared.Options;

namespace SupportCenter.Shared.Extensions;
public static class SemanticKernelHostingExtensions
{
    public static ISemanticTextMemory CreateMemory(IServiceProvider provider, string agent)
    {
        OpenAIOptions openAiConfig = provider.GetService<IOptions<OpenAIOptions>>()?.Value ?? new OpenAIOptions();
        openAiConfig.ValidateRequiredProperties();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole()
            .AddDebug();
        });
        if (agent == "Invoice")
        {
            var aiSearchConfig = provider.GetService<IOptions<AISearchOptions>>()?.Value ?? new AISearchOptions();
            aiSearchConfig.ValidateRequiredProperties();

            var memoryBuilder = new MemoryBuilder();
            return memoryBuilder.WithLoggerFactory(loggerFactory)
                            .WithMemoryStore(new AzureAISearchMemoryStore(aiSearchConfig.SearchEndpoint!, aiSearchConfig.SearchKey!))
                            // IMPROVE: maybe with a dependency injection container:
                            // https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/embedding-generation?pivots=programming-language-csharp#constructing-an-embedding-generator
                            .WithTextEmbeddingGeneration(new AzureOpenAITextEmbeddingGenerationService(openAiConfig.EmbeddingsDeploymentOrModelId, openAiConfig.EmbeddingsEndpoint, openAiConfig.EmbeddingsApiKey))
                            .Build();
        }
        else
        {
            var qdrantConfig = provider.GetService<IOptions<QdrantOptions>>()?.Value ?? new QdrantOptions();
            qdrantConfig.ValidateRequiredProperties();

            return new MemoryBuilder().WithLoggerFactory(loggerFactory)
                         .WithQdrantMemoryStore(qdrantConfig.Endpoint, qdrantConfig.VectorSize)
                         .WithTextEmbeddingGeneration(new AzureOpenAITextEmbeddingGenerationService(openAiConfig.EmbeddingsDeploymentOrModelId, openAiConfig.EmbeddingsEndpoint, openAiConfig.EmbeddingsApiKey))
                         .Build();
        }
    }

    public static Kernel CreateKernel(IServiceProvider provider, string agent)
    {
        var openAiConfig = provider.GetService<IOptions<OpenAIOptions>>()?.Value ?? new OpenAIOptions();

        var agentConfiguration = AgentConfiguration.GetAgentConfiguration(agent);
        agentConfiguration.ConfigureOpenAI(openAiConfig);

        var clientOptions = new AzureOpenAIClientOptions()
        {

        };
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(c => c.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Chat
        var azureOpenAIClientForChat = new AzureOpenAIClient(new Uri(openAiConfig.ChatEndpoint), new AzureKeyCredential(openAiConfig.ChatApiKey), clientOptions);
        builder.Services.AddAzureOpenAIChatCompletion(openAiConfig.ChatDeploymentOrModelId, azureOpenAIClientForChat);

        // Embeddings
        var azureOpenAIClientForEmbedding = new AzureOpenAIClient(new Uri(openAiConfig.EmbeddingsEndpoint), new AzureKeyCredential(openAiConfig.EmbeddingsApiKey), clientOptions);

        builder.Services.AddAzureOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, azureOpenAIClientForEmbedding);

        builder.Services.ConfigureHttpClientDefaults(c =>
        {
            c.AddStandardResilienceHandler().Configure(o =>
            {
                o.Retry.MaxRetryAttempts = 5;
                o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            });
        });

        var kernel = builder.Build();
        agentConfiguration.ConfigureKernel(kernel, provider);

        return kernel;
    }
}
#pragma warning restore SKEXP0050
