# Weather App Sample

A sample MCP App that displays weather information with an interactive UI.

## What Are MCP Apps?

[MCP Apps](https://blog.modelcontextprotocol.io/posts/2026-01-26-mcp-apps/) let tools return interactive interfaces instead of plain text. When a tool declares a UI resource, the host renders it in a sandboxed iframe where users can interact directly.

### MCP Apps = Tool + UI Resource

The architecture relies on two MCP primitives:

1. **Tools** with UI metadata pointing to a resource URI
2. **Resources** containing bundled HTML/JavaScript served via the `ui://` scheme

Azure Functions makes it easy to build both.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for building the UI)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An MCP-compatible host (Claude Desktop, VS Code, ChatGPT, etc.)

## Getting Started

### 1. Build the UI

The UI must be bundled before running the function app:

```bash
cd app
npm install
npm run build
cd ..
```

This creates a bundled `app/dist/index.html` file that the function serves.

### 2. Run the Function App

```bash
func start
```

The MCP server will be available at `http://localhost:7071/runtime/webhooks/mcp`.

### 3. Connect from VS Code

Open **.vscode/mcp.json**. Find the server called _local-mcp-function_ and click **Start** above the name. The server is already set up with the running Function app's MCP endpoint:

```shell
http://localhost:7071/runtime/webhooks/mcp
```

### 4.Prompt the Agent

Ask Copilot: "What's the weather in Seattle?"

## Source Code

The source code is in [WeatherFunction.cs](WeatherFunction.cs). The key concept is how **tools connect to resources via metadata**.

### The Tool with UI Metadata

The `GetWeather` tool uses `[McpMetadata]` to declare it has an associated UI:

```csharp
// Required metadata
private const string ToolMetadata = """
    {
        "ui": {
            "resourceUri": "ui://weather/index.html"
        }
    }
    """;

[Function(nameof(GetWeather))]
public async Task<object> GetWeather(
    [McpToolTrigger(nameof(GetWeather), "Returns current weather for a location via Open-Meteo.")]
    [McpMetadata(ToolMetadata)]
    ToolInvocationContext context,
    [McpToolProperty("location", "City name to check weather for (e.g., Seattle, New York, Miami)")]
    string location)
{
    var result = await _weatherService.GetCurrentWeatherAsync(location);
    return result;
}
```

The `resourceUri` points to `ui://weather/index.html`— this tells the MCP host that when this tool is invoked, there's an interactive UI available at that resource URI.

### The Resource Serving the UI

The `GetWeatherWidget` function serves the bundled HTML at the matching URI:

```csharp
// Optional UI metadata
private const string ResourceMetadata = """
    {
        "ui": {
            "prefersBorder": true
        }
    }
    """;

[Function(nameof(GetWeatherWidget))]
public string GetWeatherWidget(
    [McpResourceTrigger(
        "ui://weather/index.html",
        "Weather Widget",
        MimeType = "text/html;profile=mcp-app",
        Description = "Interactive weather display for MCP Apps")]
    [McpMetadata(ResourceMetadata)]
    ResourceInvocationContext context)
{
    var file = Path.Combine(AppContext.BaseDirectory, "app", "dist", "index.html");
    return File.ReadAllText(file);
}
```

### How It Works Together

1. User asks: "What's the weather in Seattle?"
2. Agent calls the `GetWeather` tool
3. Tool returns weather data (JSON) **and** the host sees the `ui.resourceUri` metadata
4. Host fetches the UI resource from `ui://weather/index.html`
5. Host renders the HTML in a sandboxed iframe, passing the tool result as context
6. User sees an interactive weather widget instead of plain text

### The UI (TypeScript)

The frontend in `app/src/weather-app.ts` receives the tool result and renders the weather display. It's bundled with Vite into a single `index.html` that the resource serves.

### Deploy

Follow [instructions](https://github.com/Azure-Samples/remote-mcp-functions-dotnet/blob/main/README.md#weather-mcp-app) in the main README to deploy the app. 
