using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static FunctionsSnippetTool.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Demonstrate how you can define tool properties without requiring
// input bindings:
builder
    .ConfigureMcpTool(GetSnippetToolName)
    .WithProperty(SnippetNamePropertyName, McpToolPropertyType.String, SnippetNamePropertyDescription, required: true);

builder.Build().Run();
