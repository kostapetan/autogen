// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

using SupportCenter.Agents.CustomerInfo;
using SupportCenter.Agents.Discount;
using SupportCenter.Agents.Dispatcher;
using SupportCenter.Agents.Invoice;
using SupportCenter.Agents.QnA;
using SupportCenter.Agents.Services;
using SupportCenter.Agents.SignalR;
using SupportCenter.ServiceDefaults;
using SupportCenter.Shared.Extensions;
using SupportCenter.Shared.Hubs;
using Microsoft.AutoGen.Core;
using Microsoft.AutoGen.Core.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
//.AddNamedAzureSignalR("signalr"); ;

builder.AddGrpcAgentWorker(builder.Configuration["AGENT_HOST"]!)
    .AddAgent<Dispatcher>("dispatcher")
    .AddAgent<CustomerInfo>("customerInfo")
    .AddAgent<Discount>("discount")
    .AddAgent<Invoice>("invoice")
    .AddAgent<QnA>("qna")
    .AddAgent<SignalRAgent>("signalr-hub");

builder.Services.AddSingleton<ISignalRService, SignalRService>();

// Allow any CORS origin if in DEV
const string AllowDebugOriginPolicy = "AllowDebugOrigin";
const string AllowOriginPolicy = "AllowOrigin";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowDebugOriginPolicy, builder =>
        {
            builder
            .WithOrigins()
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
    });
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowOriginPolicy, builder =>
        {
            builder
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithOrigins("https://*.azurecontainerapps.io") // client url
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });
}

builder.Services.ExtendOptions();
builder.Services.ExtendServices();
builder.Services.RegisterSemanticKernelNativeFunctions();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseRouting();
if (builder.Environment.IsDevelopment())
{
    app.UseCors(AllowDebugOriginPolicy);
}
else
{
    app.UseCors(AllowOriginPolicy);
}
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Support Center APIs v1");
});

app.MapHub<SupportCenterHub>("/supportcenterhub");
app.Run();
