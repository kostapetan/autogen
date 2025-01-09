// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

using Marketing.Backend.Agents;
using Marketing.Backend.Hubs;
using Microsoft.AutoGen.Core;
using Marketing.Backend.Agents.CommunityManager;
using Marketing.Backend.Agents.GraphicDesigner;
using Marketing.Backend.Agents.Writer;
using Marketing.Backend.Agents.Auditor;
using Microsoft.AutoGen.Core.Grpc;
using Marketing.ServiceDefaults;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddSingleton(
    new AzureOpenAIClient(
        new Uri(builder.Configuration["OpenAI:Endpoint"]!),
        new ApiKeyCredential(builder.Configuration["OpenAI:Key"]!)
    ));

builder.Services.AddChatClient(s => s.GetRequiredService<AzureOpenAIClient>().AsChatClient("gpt-4o-mini"));
                                

builder.AddGrpcAgentWorker(builder.Configuration["AGENT_HOST"]!)
    .AddAgentHost()
    .AddAgent<Writer>(nameof(Writer))
    .AddAgent<GraphicDesigner>(nameof(GraphicDesigner))
    .AddAgent<Auditor>(nameof(Auditor))
    .AddAgent<CommunityManager>(nameof(CommunityManager))
    .AddAgent<SignalRAgent>(nameof(SignalRAgent));

builder.Services.AddSingleton<ISignalRService, SignalRService>();

// Allow any CORS origin if in DEV
const string AllowDebugOriginPolicy = "AllowDebugOrigin";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowDebugOriginPolicy, builder =>
        {
            builder
            .WithOrigins("*") // client url
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseRouting();
app.UseCors(AllowDebugOriginPolicy);
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});

app.MapHub<ArticleHub>("/articlehub");
app.Run();
