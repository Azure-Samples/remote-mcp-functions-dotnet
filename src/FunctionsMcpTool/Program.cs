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

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Services.AddSingleton(_ => new BlobServiceClient(
    Environment.GetEnvironmentVariable("AzureWebJobsStorage")));

// Demonstrate how you can define tool properties in Program.cs
// without requiring McpToolProperty input binding attributes:
builder
    .ConfigureMcpTool(EchoToolName)
    .WithProperty(EchoMessagePropertyName, McpToolPropertyType.String, EchoMessagePropertyDescription, required: true);

// Demonstrate explicit JSON input and output schemas (Worker.Extensions.Mcp 1.5.0+).
// The tool's arguments and the shape of its structured output are advertised to MCP
// clients exactly as written here, instead of being inferred from attributes or POCOs.
builder
    .ConfigureMcpTool(SearchSnippetsToolName)
    .WithInputSchema("""
        {
            "type": "object",
            "properties": {
                "prefix": {
                    "type": "string",
                    "description": "Snippet name prefix to match. Empty string matches all snippets."
                },
                "limit": {
                    "type": "integer",
                    "description": "Maximum number of results to return (1-100).",
                    "minimum": 1,
                    "maximum": 100,
                    "default": 10
                }
            },
            "required": ["prefix"]
        }
        """)
    .WithOutputSchema("""
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "The prefix that was searched."
                },
                "count": {
                    "type": "integer",
                    "description": "Number of snippets returned."
                },
                "results": {
                    "type": "array",
                    "description": "Names of matching snippets.",
                    "items": { "type": "string" }
                }
            },
            "required": ["query", "count", "results"]
        }
        """);

builder.Build().Run();
