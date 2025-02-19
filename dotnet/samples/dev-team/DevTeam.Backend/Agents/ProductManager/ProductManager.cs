// Copyright (c) Microsoft Corporation. All rights reserved.
// ProductManager.cs

using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Backend.Agents.ProductManager;

[TopicSubscription(Consts.TopicName)]
public class ProductManager([FromKeyedServices("AgentsMetadata")] AgentsMetadata typeRegistry, IChatClient chat, ILogger<ProductManager> logger)
    : AiAgent<ProductManagerState>(typeRegistry, chat, logger), IManageProducts,
    IHandle<ReadmeChainClosed>,
    IHandle<ReadmeRequested>
{
    public async Task Handle(ReadmeChainClosed item, CancellationToken cancellationToken = default)
    {
        // TODO: Get readme from state
        var lastReadme = ""; // _state.State.History.Last().Message
        var evt = new ReadmeCreated
        {
            Readme = lastReadme
        };
        await PublishEventAsync(evt, topic: Consts.TopicName);
    }

    public async Task Handle(ReadmeRequested item, CancellationToken cancellationToken = default)
    {
        var readme = await CreateReadme(item.Ask);
        var evt = new ReadmeGenerated
        {
            Readme = readme,
            Org = item.Org,
            Repo = item.Repo,
            IssueNumber = item.IssueNumber
        };
        await PublishEventAsync(evt, topic: Consts.TopicName);
    }

    public async Task<string> CreateReadme(string ask)
    {
        try
        {
            //var context = new KernelArguments { ["input"] = AppendChatHistory(ask) };
            //var instruction = "Consider the following architectural guidelines:!waf!";
            //var enhancedContext = await AddKnowledge(instruction, "waf", context);
            return await CallFunction(PMSkills.Readme);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating readme");
            return "";
        }
    }
}

public interface IManageProducts
{
    public Task<string> CreateReadme(string ask);
}
