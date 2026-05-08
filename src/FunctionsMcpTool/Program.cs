using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using static FunctionsMcpTool.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var otel = builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    otel.UseAzureMonitorExporter();
}

builder.Services.AddSingleton(_ => new BlobServiceClient(
    Environment.GetEnvironmentVariable("AzureWebJobsStorage")));

// Demonstrate how you can define tool properties in Program.cs
// without requiring McpToolProperty input binding attributes:
builder
    .ConfigureMcpTool(EchoToolName)
    .WithProperty(EchoMessagePropertyName, McpToolPropertyType.String, EchoMessagePropertyDescription, required: true);


builder.Build().Run();
