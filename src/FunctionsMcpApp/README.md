# FunctionsMcpApp — MCP Apps with Fluent API on Azure Functions (.NET/C#)

This project demonstrates the MCP Apps fluent API (`v1.5.0-preview.1`) for building MCP tools that return interactive UI alongside data. Tools are configured with views, permissions, CSP policies, and static assets entirely in `Program.cs`.

## What are MCP Apps?

[MCP Apps](https://blog.modelcontextprotocol.io/posts/2026-01-26-mcp-apps/) let tools return interactive interfaces instead of plain text. When a tool declares a UI resource, the host renders it in a sandboxed iframe where users can interact directly.

## Tools included

| Tool | Type | Description |
|------|------|-------------|
| `HelloApp` | MCP App | Simple greeting app with a file-backed HTML view |
| `SnippetDashboard` | MCP App | Dashboard with clipboard permissions, CSP config, and static assets |
| `GetServerTime` | Standard tool | Returns current UTC time — shows tools and apps coexist |

## Key concepts

### Fluent API configuration

Instead of decorating functions with metadata attributes, the fluent API in `Program.cs` configures everything:

```csharp
builder.ConfigureMcpTool("HelloApp")
    .AsMcpApp(app => app
        .WithView("assets/hello-app.html")
        .WithTitle("Hello App")
        .WithBorder());
```

### Full-featured app example

The `SnippetDashboard` shows the full API surface:

```csharp
builder.ConfigureMcpTool("SnippetDashboard")
    .AsMcpApp(app => app
        .WithView("assets/dashboard.html")
        .WithTitle("Snippet Dashboard")
        .WithPermissions(McpAppPermissions.ClipboardWrite | McpAppPermissions.ClipboardRead)
        .WithCsp(csp =>
        {
            csp.ConnectTo("https://api.example.com")
               .LoadResourcesFrom("https://cdn.example.com");
        })
        .ConfigureApp()
        .WithStaticAssets("assets")
        .WithVisibility(McpVisibility.Model | McpVisibility.App));
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.0.7030`
- An MCP-compatible host that supports MCP Apps (VS Code with GitHub Copilot, Claude Desktop, etc.)

## Run locally

From this directory (`src/FunctionsMcpApp`), start the Functions host:

```shell
func start
```

The MCP endpoint will be available at `http://localhost:7071/runtime/webhooks/mcp`.

## Source code

- **`AppTools.cs`** — Tool functions that define the logic for each tool
- **`Program.cs`** — Fluent API configuration that wires tools to views, permissions, and CSP policies
- **`assets/`** — HTML views served as MCP App UI resources
