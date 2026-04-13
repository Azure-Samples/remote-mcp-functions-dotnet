# FunctionsMcpApp — MCP Apps with Fluent API on Azure Functions (.NET/C#)

This project demonstrates the MCP Apps fluent API (`v1.5.0-preview.1`) for building MCP tools that return interactive UI alongside data. Tools are configured with views, permissions, CSP policies, and static assets entirely in `Program.cs`.

## What are MCP Apps?

[MCP Apps](https://blog.modelcontextprotocol.io/posts/2026-01-26-mcp-apps/) let tools return interactive interfaces instead of plain text. When a tool declares a UI resource, the host renders it in a sandboxed iframe where users can interact directly.

## Tools included

| Tool | Type | Description |
|------|------|-------------|
| `HelloApp` | MCP App (static) | Simple greeting with a file-backed HTML view — shows the simplest case |
| `SnippetDashboard` | MCP App (dynamic) | Dashboard that renders live server data from the tool response using a Vite-bundled TypeScript app |
| `GetServerTime` | Standard tool | Returns current UTC time — shows tools and apps coexist |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (for building the dashboard UI)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.5.0`
- An MCP-compatible host that supports MCP Apps (VS Code with GitHub Copilot, Claude Desktop, etc.)

## Run locally

### 1. Build the dashboard UI

```shell
cd app
npm install
npm run build
cd ..
```

### 2. Start the Functions host

```shell
func start
```

The MCP endpoint will be available at `http://localhost:7071/runtime/webhooks/mcp`.

### 3. Connect and test

Open **.vscode/mcp.json**, start the server, then ask the agent to open the dashboard.

## Dynamic data rendering

The `SnippetDashboard` demonstrates that the fluent API fully supports dynamic content — the same way `McpWeatherApp` works without the fluent API.

### How it works

1. **Tool returns data** — `AppTools.cs:SnippetDashboard` returns JSON with runtime info, memory usage, uptime, and chart metrics
2. **HTML receives the tool result** — `app/src/dashboard-app.ts` uses `@modelcontextprotocol/ext-apps` SDK to listen for `ontoolresult`
3. **UI renders dynamically** — The TypeScript app parses the JSON and updates tiles, bar charts, and status indicators

The key insight: MCP App rendering is independent of how you configure the tool. Whether you use the fluent API (`ConfigureMcpTool().AsMcpApp()`) or metadata attributes (`[McpMetadata]`), the UI receives the tool result the same way. Build your frontend however you like — static HTML, Vite + TypeScript, React, or any framework.

### Fluent API configuration

```csharp
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
```

### The tool function (returns dynamic data)

```csharp
[Function(nameof(SnippetDashboard))]
public string SnippetDashboard(
    [McpToolTrigger("SnippetDashboard", "Opens a snippet dashboard with live server metrics.")]
        ToolInvocationContext context)
{
    var process = System.Diagnostics.Process.GetCurrentProcess();
    return JsonSerializer.Serialize(new
    {
        Status = "Online",
        Runtime = RuntimeInformation.FrameworkDescription,
        Environment = RuntimeInformation.OSDescription,
        MemoryMB = process.WorkingSet64 / (1024 * 1024),
        // ... plus chart metrics
    });
}
```

### The UI (TypeScript with ext-apps SDK)

```typescript
import { App } from "@modelcontextprotocol/ext-apps";

const app = new App({ name: "Snippet Dashboard", version: "1.0.0" });

app.ontoolresult = (params) => {
  const data = JSON.parse(params.content[0].text);
  render(data);  // Update tiles, charts, status indicators
};

await app.connect();
```

### Including assets in the build output

Azure Functions serves files from the build output directory, so any HTML views or bundled assets must be copied there. Add entries in your `.csproj` file to include them:

```xml
<!-- Static HTML views (e.g. HelloApp) -->
<Content Include="assets\**\*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>

<!-- Vite-bundled dashboard app -->
<None Update="app\dist\**\*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

Without these entries the files won't be present at runtime and `WithView()` / `WithStaticAssets()` will fail to locate them.

## Static vs Dynamic — side by side

| | `HelloApp` (static) | `SnippetDashboard` (dynamic) |
|---|---|---|
| **View** | Plain HTML file | Vite-bundled TypeScript app |
| **Data** | Hardcoded in HTML | Received via `ontoolresult` |
| **Build step** | None | `npm run build` |
| **Use case** | Simple info cards, help pages | Dashboards, charts, forms, live data |


## Building real-world apps

This pattern scales to any frontend framework:

- **Charts & data visualization** — Return metrics from the tool, render with Chart.js, D3, or plain SVG
- **Forms & data collection** — Return a form schema, render inputs, collect responses
- **Adaptive cards** — Return structured card data, render with the Adaptive Cards SDK
- **Approval workflows** — Return pending items, render approve/reject buttons

The approach is always the same:

1. The **C# tool function** gathers and returns data
2. The **HTML/JS frontend** receives it via `ontoolresult` and renders the UI
3. The **fluent API** configures the plumbing (view path, CSP, permissions)

## Source code

- **`AppTools.cs`** — Tool functions that define the logic for each tool
- **`Program.cs`** — Fluent API configuration that wires tools to views, permissions, and CSP policies
- **`FunctionsMcpApp.csproj`** — Project file with asset copy rules for static and bundled views
- **`app/`** — Vite + TypeScript dashboard app using `@modelcontextprotocol/ext-apps`
- **`assets/`** — Static HTML views (HelloApp)
