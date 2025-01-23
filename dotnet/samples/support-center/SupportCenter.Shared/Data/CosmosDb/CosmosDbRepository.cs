// Copyright (c) Microsoft Corporation. All rights reserved.
// CosmosDbRepository.cs

using System.ComponentModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SupportCenter.Shared.Data.Entities;
using SupportCenter.Shared.Options;

namespace SupportCenter.Shared.Data.CosmosDb;

public abstract class CosmosDbRepository<TEntity, TOptions>
    where TEntity : Entity
    where TOptions : CosmosDbOptions
{
    protected readonly ILogger Logger;
    protected readonly Microsoft.Azure.Cosmos.Container Container;

    protected CosmosDbRepository(TOptions options, ILogger logger)
    {
        Logger = logger;
        CosmosDbOptions configuration = options;

        var containerConfiguration = configuration.Containers?.FirstOrDefault(c => c.EntityName == typeof(TEntity).Name)
            ?? throw new InvalidOperationException($"Container configuration for {typeof(TEntity).Name} not found.");

        var client = new CosmosClient(configuration.AccountUri, configuration.AccountKey);
        client.CreateDatabaseIfNotExistsAsync(containerConfiguration.DatabaseName);

        var database = client.GetDatabase(containerConfiguration.DatabaseName);
        database.CreateContainerIfNotExistsAsync(containerConfiguration.ContainerName, containerConfiguration.PartitionKey ?? "/partitionKey");

        Container = database.GetContainer(containerConfiguration.ContainerName);
    }

    public async Task<TOutput> GetItemAsync<TOutput>(string id, string partitionKey)
    {
        TOutput item = await Container.ReadItemAsync<TOutput>(id: id, partitionKey: new PartitionKey(partitionKey));
        return item;
    }

    public async Task InsertItemAsync(TEntity entity)
    {
        try
        {
            var response = await Container.CreateItemAsync(entity, new PartitionKey(entity.GetPartitionKeyValue()));
        }
        catch (Exception ex)
        {
            Logger.LogCritical(
                ex,
                "An error occurred. MethodName: {methodName} ErrorMessage: {errorMessage}",
                nameof(InsertItemAsync),
                ex.Message
            );

            throw;
        }
    }

    public async Task UpsertItemAsync(TEntity entity)
    {
        await Container.UpsertItemAsync(entity, new PartitionKey(entity.GetPartitionKeyValue()));
    }
}
