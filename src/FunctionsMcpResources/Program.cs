using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static FunctionsMcpResources.ResourcesInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure metadata on resources:
builder
    .ConfigureMcpResource(ServerInfoResourceUri)
    .WithMetadata("cache", new { ttlSeconds = 60 });

builder.Build().Run();
