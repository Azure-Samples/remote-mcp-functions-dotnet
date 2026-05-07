using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static FunctionsMcpPrompts.PromptsInformation;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

// Configure prompt arguments in Program.cs
// without requiring McpPromptArgument input binding attributes:
builder
    .ConfigureMcpPrompt(GenerateDocsPromptName)
    .WithArgument(GenerateDocsFunctionNameArgName, GenerateDocsFunctionNameArgDescription, required: true)
    .WithArgument(GenerateDocsStyleArgName, GenerateDocsStyleArgDescription);

builder.Build().Run();
