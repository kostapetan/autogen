// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

using Marketing.Backend.Agents;
using Marketing.Backend.Hubs;
using Marketing.Agents;
using Microsoft.AutoGen.Core;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.AddGrpcAgentWorker(builder.Configuration["AGENT_HOST"]!)
    .AddAgentHost()
    .AddAgent<Writer>("writer")
    .AddAgent<GraphicDesigner>("graphic-designer")
    .AddAgent<Auditor>("auditor")
    .AddAgent<CommunityManager>("community-manager")
    .AddAgent<SignalRAgent>("signalr-hub");

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
