using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static FunctionsMcpTool.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton(_ => new BlobServiceClient(
        Environment.GetEnvironmentVariable("AzureWebJobsStorage")));

// Demonstrate how you can define tool properties in Program.cs
// without requiring McpToolProperty input binding attributes:
builder
    .ConfigureMcpTool(EchoToolName)
    .WithProperty(EchoMessagePropertyName, McpToolPropertyType.String, EchoMessagePropertyDescription, required: true);

// Demonstrate how you can define prompt arguments in Program.cs
// without requiring McpPromptArgument input binding attributes:
builder
    .ConfigureMcpPrompt(GenerateDocsPromptName)
    .WithArgument(GenerateDocsFunctionNameArgName, GenerateDocsFunctionNameArgDescription, required: true)
    .WithArgument(GenerateDocsStyleArgName, GenerateDocsStyleArgDescription);

// Demonstrate how you can configure metadata on a resource:
builder
    .ConfigureMcpResource(ServerInfoResourceUri)
    .WithMetadata("cache", new { ttlSeconds = 60 });

builder.Build().Run();
