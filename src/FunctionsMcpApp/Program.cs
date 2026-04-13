using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// ─── MCP App: HelloApp ─────────────────────────────────────────────
// Simple app with a file-backed view and a title.
builder.ConfigureMcpTool("HelloApp")
    .AsMcpApp(app => app
        .WithView("assets/hello-app.html")
        .WithTitle("Hello App")
        .WithBorder());

// ─── MCP App: SnippetDashboard ─────────────────────────────────────
// Dynamic dashboard built with Vite + @modelcontextprotocol/ext-apps SDK.
// The bundled HTML receives tool results via ontoolresult and renders live data.
builder.ConfigureMcpTool("SnippetDashboard")
    .AsMcpApp(app => app
        .WithView("app/dist/index.html")
        .WithTitle("Snippet Dashboard")
        .WithPermissions(McpAppPermissions.ClipboardWrite | McpAppPermissions.ClipboardRead)
        .WithCsp(csp =>
        {
            csp.ConnectTo("https://api.example.com")
               .LoadResourcesFrom("https://cdn.example.com");
        })
        .ConfigureApp()
        .WithStaticAssets("app/dist")
        .WithVisibility(McpVisibility.Model | McpVisibility.App));

builder.Build().Run();

