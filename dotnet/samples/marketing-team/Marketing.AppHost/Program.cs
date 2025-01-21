// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

using Azure.Provisioning;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var agentHost = builder.AddContainer("agent-host", "kpetan.azurecr.io/autogen/agent-host", "v1.4")
                       //.AddContainer("agent-host", "autogen/agent-host", "latest")
                       .WithEnvironment("ASPNETCORE_URLS", "https://+;http://+")
                       .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "5001")
                       .AsHttp2Service()
                       //.WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Password", "mysecurepass")
                       //.WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Path", "/https/devcert.pfx")
                       //.WithBindMount("./certs", "/https/", true)
                       .WithHttpsEndpoint(targetPort: 5001)
                       .PublishAsAzureContainerApp((infra, ca) => { });

var agentHostHttps = agentHost.GetEndpoint("https");

var cache = builder.AddRedis("cache");

var signalr = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSignalR("signalr")
    : builder.AddConnectionString("signalr");

var backend = builder.AddProject<Projects.Marketing_Backend>("backend")
    .WithEnvironment("AGENT_HOST", $"{agentHostHttps.Property(EndpointProperty.Url)}")
    .WithEnvironment("OpenAI__Key", builder.Configuration["OpenAI:Key"])
    .WithEnvironment("OpenAI__Endpoint", builder.Configuration["OpenAI:Endpoint"])
    .WithExternalHttpEndpoints()
    .WithReference(signalr)
    .WithReference(cache)
    .WithReplicas(2)
    .WaitFor(agentHost)
    .PublishAsAzureContainerApp((infra, ca) =>
    {
        ca.Configuration.Ingress.CorsPolicy = new ContainerAppCorsPolicy
        {
            AllowCredentials = true,
            AllowedOrigins = new BicepList<string> { "https://*.azurecontainerapps.io" },
            AllowedHeaders = new BicepList<string> { "*" },
            AllowedMethods = new BicepList<string> { "*" }
        };
        ca.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        //ca.Configuration.Secrets.Add("OpenAI__Key", builder.Configuration["OpenAI:Key"]);

    });

builder.AddNpmApp("frontend", "../Marketing.Frontend", "dev")
    .WithReference(backend)
    .WithEnvironment("NEXT_PUBLIC_BACKEND_URI", backend.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
