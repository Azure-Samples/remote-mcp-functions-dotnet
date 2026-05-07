# FunctionsMcpApp — MCP Apps with Fluent API on Azure Functions (.NET/C#)

This project demonstrates the MCP Apps fluent API (`v1.5.0`) for building MCP tools that return interactive UI alongside data. Tools are configured with views, permissions, CSP policies, and static assets entirely in `Program.cs`.

## What are MCP Apps?

[MCP Apps](https://modelcontextprotocol.io/extensions/apps/overview) let tools return interactive interfaces instead of plain text. When a tool declares a UI resource, the host renders it in a sandboxed iframe where users can interact directly.

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
- [Docker](https://www.docker.com/) (for the Azurite storage emulator)
- An MCP-compatible host that supports MCP Apps (VS Code with GitHub Copilot, Claude Desktop, etc.)

## Prepare your local environment

An Azure Storage Emulator is needed for the Functions runtime. Start Azurite:

```shell
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
    mcr.microsoft.com/azure-storage/azurite
```

> If you use the Azurite VS Code extension instead, run **Azurite: Start** now.

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

### 3. Connect from VS Code 

Open **.vscode/mcp.json** in the workspace root. Find the server called **`local-mcp-function`** and click **Start** above the name. It points to:

```
http://localhost:7071/runtime/webhooks/mcp
```

In Copilot chat **agent** mode, ask the agent to open the dashboard or get the server time.

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

## Deploy to Azure

### Step 1: Sign in

```shell
az login
azd auth login
```

### Step 2: Create an environment

```shell
azd env new <environment-name>
```

This also becomes the resource group name. 

### Step 3: Provision and deploy

By default, OAuth-based authentication is enabled using the [built-in MCP auth feature](https://learn.microsoft.com/azure/app-service/configure-authentication-mcp?toc=/azure/azure-functions/toc.json&bc=/azure/azure-functions/breadcrumb/toc.json) with Microsoft Entra as the identity provider.

Configure VS Code as an allowed client application for Microsoft Entra:

```shell
azd env set PRE_AUTHORIZED_CLIENT_IDS aebc6443-996d-45c2-90f0-388ff96faa56
```

Optionally enable VNet isolation:

```shell
azd env set VNET_ENABLED true
```

Deploy the project. When prompted, pick your subscription and an Azure region.

```shell
azd up
```

### Connect to the remote MCP server

Open **`.vscode/mcp.json`** and click **Start** above the remote server entry for this project. You'll be prompted for `functionapp-name` — find it in your `azd` command output or the `.azure/<env>/.env` file. Since authentication is enabled, you'll also be prompted to sign in with Microsoft.

> **Tip:** A successful connection shows the number of tools the server exposes. Click **More... → Show Output** above the server name to see request/response details.

### Redeploy and clean up

- **Redeploy:** `azd deploy`
- **Clean up all resources:** `azd down`

### Other auth options 

#### Key-based access

1. Set the auth level to `function` in `FunctionsMcpApp/host.json`:

    ```json
    "extensions": {
        "mcp": {
            "system": {
                "webhookAuthorizationLevel": "function"
            }
        }    
    },
    ```

2. Disable built-in MCP auth before deploying:

    ```shell
    azd env set ENABLE_AUTH false
    ```

3. Deploy with `azd up`.

4. Get the MCP extension system key from the Azure portal or CLI:

    ```shell
    az functionapp keys list --name <functionapp-name> --resource-group <resource-group> --query "systemKeys.mcp_extension" -o tsv
    ```

5. Add a key-based server entry to `.vscode/mcp.json` (VS Code will prompt you for both values on connect):

    ```jsonc
    {
        "servers": {
            "remote-functions-mcp-key": {
                "type": "http",
                "url": "https://${input:functionapp-name}.azurewebsites.net/runtime/webhooks/mcp",
                "headers": {
                    "x-functions-key": "${input:functions-mcp-extension-system-key}"
                }
            }
        },
        "inputs": [
            {
                "type": "promptString",
                "id": "functionapp-name",
                "description": "Azure Functions app name"
            },
            {
                "type": "promptString",
                "id": "functions-mcp-extension-system-key",
                "description": "Azure Functions MCP extension system key",
                "password": true
            }
        ]
    }
    ```

#### Anonymous access

To disable authentication entirely, set the following variable _before_ running `azd up`:

```shell
azd env set ENABLE_AUTH false
```

Then deploy with `azd up`. Anyone will be able to connect to the remote MCP server. This is **not** recommended unless the server is meant to be accessible by anyone (for example, serves publicly available info or data).

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `azd up` provision succeeded but deploy immediately failed: `unable to find a resource tagged with 'azd-service-name: mcp'` | The tag was provisioned but not propagated yet when `azd deploy` looked it up — run `azd deploy` again |
| `azd deploy` fails with Kudu restart error: `deployment was partially successful: [KuduSpecializer] Kudu has been restarted after package deployed` | Transient error — run `azd deploy` again |

## Source code

- **`AppTools.cs`** — Tool functions that define the logic for each tool
- **`Program.cs`** — Fluent API configuration that wires tools to views, permissions, and CSP policies
- **`assets/`** — HTML views served as MCP App UI resources
- **`FunctionsMcpApp.csproj`** — Project file with asset copy rules for static and bundled views
- **`app/`** — Vite + TypeScript dashboard app using `@modelcontextprotocol/ext-apps`
- **`assets/`** — Static HTML views (HelloApp)


## Next Steps
+ Learn more about the [Azure Functions MCP extension](https://learn.microsoft.com/azure/azure-functions/functions-bindings-mcp?pivots=programming-language-typescript)
+ Connect your MCP server to [Foundry agents](https://learn.microsoft.com/azure/azure-functions/functions-mcp-foundry-tools?tabs=oauth-id%2Cmcp-extension%2Cfoundry)
+ Add [API Management](https://github.com/Azure-Samples/remote-mcp-apim-functions-python) to your MCP server

