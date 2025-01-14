// Copyright (c) Microsoft Corporation. All rights reserved.
// Program.cs

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var agentHost = builder.AddContainer("agent-host", "kpetan.azurecr.io/autogen/agent-host","v1")
                       .WithEnvironment("ASPNETCORE_URLS", "https://+;http://+")
                       .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "5001")
                       .AsHttp2Service()
                       //.WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Password", "mysecurepass")
                       //.WithEnvironment("ASPNETCORE_Kestrel__Certificates__Default__Path", "/https/devcert.pfx")
                       //.WithBindMount("./certs", "/https/", true)
                       .WithHttpsEndpoint(targetPort: 5001);

var agentHostHttps = agentHost.GetEndpoint("https");

var backend = builder.AddProject<Projects.Marketing_Backend>("backend")
    .WithEnvironment("AGENT_HOST", $"{agentHostHttps.Property(EndpointProperty.Url)}")
    .WithEnvironment("OpenAI__Key", builder.Configuration["OpenAI:Key"])
    .WithEnvironment("OpenAI__Endpoint", builder.Configuration["OpenAI:Endpoint"])
    .WaitFor(agentHost);

builder.AddNpmApp("frontend", "../Marketing.Frontend", "dev")
    .WithReference(backend)
    .WithEnvironment("NEXT_PUBLIC_BACKEND_URI", backend.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
