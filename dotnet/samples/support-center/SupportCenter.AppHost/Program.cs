// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

/* Agent Host */
//var agentHost = builder.AddProject<Projects.SupportCenter_AgentHost>("agentHost")
//    .WithReference(orleans);
//var agentHostHttps = agentHost.GetEndpoint("https");

var cache = builder.AddRedis("cache");

var signalr = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSignalR("signalr")
    : builder.AddConnectionString("signalr");

/* Backend */
var backend = builder.AddProject<Projects.SupportCenter_Backend>("backend")
    .WithEnvironment("OpenAI__Key", builder.Configuration["OpenAIOptions:Key"])
    .WithEnvironment("OpenAI__Endpoint", builder.Configuration["OpenAIOptions:Endpoint"])
    .WithExternalHttpEndpoints()
    .WithReference(signalr)
    .WithReference(cache)
    .WithReplicas(2);
//.PublishAsDockerFile();
//.PublishAsAzureContainerApp((infra, ca) =>
//{
//    ca.Configuration.Ingress.CorsPolicy = new ContainerAppCorsPolicy
//    {
//        AllowCredentials = true,
//        AllowedOrigins = new BicepList<string> { "https://*.azurecontainerapps.io" },
//        AllowedHeaders = new BicepList<string> { "*" },
//        AllowedMethods = new BicepList<string> { "*" }
//    };
//    ca.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
//    //ca.Configuration.Secrets.Add("OpenAI__Key", builder.Configuration["OpenAI:Key"]);

//});

/* Frontend */
builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "dev")
    .WithReference(backend)
    .WithEnvironment("VITE_OAGENT_BASE_URL", backend.GetEndpoint("http"))
    .WithEnvironment("VITE_IS_MOCK_ENABLED", "true")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
