using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static FunctionsMcpResources.ResourcesInformation;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var otel = builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    otel.UseAzureMonitorExporter();
}

// Configure metadata on resources:
builder
    .ConfigureMcpResource(ServerInfoResourceUri)
    .WithMetadata("cache", new { ttlSeconds = 60 });

builder.Build().Run();
