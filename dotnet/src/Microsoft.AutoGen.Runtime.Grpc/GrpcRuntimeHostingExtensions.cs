// Copyright (c) Microsoft Corporation. All rights reserved.
// GrpcRuntimeHostingExtensions.cs

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans.Serialization;

namespace Microsoft.AutoGen.Runtime.Grpc;

public static class GrpcRuntimeHostingExtensions
{
    internal static WebApplicationBuilder AddOrleans(this WebApplicationBuilder builder, bool inMemory = false)
    {
        builder.Services.AddSerializer(serializer => serializer.AddProtobufSerializer());

        // Ensure Orleans is added before the hosted service to guarantee that it starts first.
        //TODO: make all of this configurable
        builder.UseOrleans((siloBuilder) =>
        {
            // Development mode or local mode uses in-memory storage and streams
            if (inMemory)
            {
                siloBuilder.UseLocalhostClustering()
                  .AddMemoryStreams("StreamProvider")
                  .AddMemoryGrainStorage("PubSubStore")
                  .AddMemoryGrainStorage("AgentsStore");
            }

            siloBuilder.UseInMemoryReminderService();
            //TODO: Add pass through config for state and streams
        });

        return builder;
    }
    public static WebApplicationBuilder AddGrpcRuntime(this WebApplicationBuilder builder, bool inMemoryOrleans = false)
    {
        // TODO: allow for configuration of Orleans, for state
        builder.AddOrleans(inMemoryOrleans);

        builder.Services.TryAddSingleton(DistributedContextPropagator.Current);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(20); // Match the client's KeepAlivePingDelay
            serverOptions.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(10); // Match the client's KeepAlivePingTimeout

            // TODO: make port configurable
            serverOptions.ListenAnyIP(5001, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                // TODO: make HTTPS configurable
                //listenOptions.UseHttps(); 
            });
        });

        builder.Services.AddGrpc();
        builder.Services.AddSingleton<GrpcGateway>();
        builder.Services.AddSingleton(sp => (IHostedService)sp.GetRequiredService<GrpcGateway>());

        return builder;
    }

    public static WebApplication MapAgentService(this WebApplication app, bool local = false, bool useGrpc = true)
    {
        if (useGrpc) { app.MapGrpcService<GrpcGatewayService>(); }
        return app;
    }
}
