// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Microsoft.AutoGen.Core;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using DevTeam.Backend.Services;
using DevTeam.Backend.Agents;
using DevTeam.Backend.Agents.ProductManager;
using DevTeam.Backend.Agents.DeveloperLead;
using DevTeam.Backend.Agents.Developer;
using DevTeam.Backend.Options;
using DevTeam.ServiceDefaults;
using Microsoft.AutoGen.Core.Grpc;
using Azure.AI.OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.AddGrpcAgentWorker(builder.Configuration["AGENT_HOST"]!)
    .AddAgentHost()
    .AddAgent<AzureGenie>(nameof(AzureGenie))
    //.AddAgent<Sandbox>(nameof(Sandbox))
    .AddAgent<Hubber>(nameof(Hubber))
    .AddAgent<Dev>(nameof(Dev))
    .AddAgent<ProductManager>(nameof(ProductManager))
    .AddAgent<DeveloperLead>(nameof(DeveloperLead));

builder.Services.AddSingleton<WebhookEventProcessor, GithubWebHookProcessor>();
builder.Services.AddSingleton<GithubAuthService>();
builder.Services.AddSingleton<IManageAzure, AzureService>();
builder.Services.AddSingleton<IManageGithub, GithubService>();

builder.Services.AddSingleton(
    new AzureOpenAIClient(
        new Uri(builder.Configuration["OpenAI:Endpoint"]!),
        new ApiKeyCredential(builder.Configuration["OpenAI:Key"]!)
    ));

builder.Services.AddChatClient(s => s.GetRequiredService<AzureOpenAIClient>().AsChatClient("gpt-4o-mini"));

builder.Services.AddTransient(s =>
{
    var ghOptions = s.GetRequiredService<IOptions<GithubOptions>>();
    var logger = s.GetRequiredService<ILogger<GithubAuthService>>();
    var ghService = new GithubAuthService(ghOptions, logger);
    var client = ghService.GetGitHubClient();
    return client;
});

// TODO: Rework?
builder.Services.AddOptions<GithubOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("Github").Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddArmClient(default);
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseRouting()
.UseEndpoints(endpoints =>
{
    var ghOptions = app.Services.GetRequiredService<IOptions<GithubOptions>>().Value;
    endpoints.MapGitHubWebhooks(secret: ghOptions.WebhookSecret);
}); ;

app.UseSwagger();
/* app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
}); */

app.Run();
