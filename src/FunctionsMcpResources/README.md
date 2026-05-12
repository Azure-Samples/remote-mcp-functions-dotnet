# FunctionsMcpResources — MCP Resource Templates on Azure Functions (.NET/C#)

This project is a .NET 10 Azure Function app that exposes MCP (Model Context Protocol) resource templates as a remote MCP server. Resource templates allow MCP clients to discover and read structured data through URI-based patterns.

> **Note:** MCP tools are in the [FunctionsMcpTool](../FunctionsMcpTool/) project, and prompts are in the [FunctionsMcpPrompts](../FunctionsMcpPrompts/) project.

## Resources included

| Resource | URI | Description |
|----------|-----|-------------|
| `ServerInfo` | `info://server` | Static resource that returns server name, version, runtime, and timestamp. |
| `Documentation` | `docs://{Topic}` | Resource template that returns documentation for a given topic. Available topics: `mcp-resources`, `mcp-tools`, `mcp-prompts`. |
| `Snippet` | `snippet://{Name}` | Resource template that reads a code snippet by name from blob storage. Requires a snippet saved in the `snippets` blob container. |

## Key concepts

- **Resource templates** have URI parameters (e.g., `{Name}`) that clients substitute at runtime — they're like parameterized endpoints.
- **Static resources** have fixed URIs and return the same structure every call.
- Resource metadata (like cache TTL) is configured in `Program.cs` via `ConfigureMcpResource`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.5.0`
- [Docker](https://www.docker.com/) (for the Azurite storage emulator — needed by the snippet resource template)

## Prepare your local environment

An Azure Storage Emulator is needed because the snippet resource reads blobs from storage. Start Azurite:

```shell
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
    mcr.microsoft.com/azure-storage/azurite
```

> If you use the Azurite VS Code extension instead, run **Azurite: Start** now.

## Run locally

From this directory (`src/FunctionsMcpResources`), start the Functions host:

```shell
func start
```

The MCP endpoint will be available at `http://localhost:7071/runtime/webhooks/mcp`.

## Connect from VS Code 

Open **.vscode/mcp.json** in the workspace root. Find the server called **`local-mcp-function`** and click **Start** above the name. It points to:

```
http://localhost:7071/runtime/webhooks/mcp
```

MCP resources are attached as context in VS Code Chat (they aren't invoked like tools or prompts):

1. Open the **Chat** panel.
2. Click the **+** (Attach) button in the chat input.
3. Select **MCP Resources**.
4. Choose a resource (e.g. **ServerInfo**, **Documentation**).

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

> **Tip:** Click **More... → Show Output** above the server name to see request/response details.

### Test your resources

MCP resources are attached as context in VS Code Chat (they aren't invoked like tools or prompts).

1. Open the **Chat** panel.
2. Click the **+** (Attach) button in the chat input.
3. Select **MCP Resources**.
4. Choose a resource:
   - **`Documentation`** — enter a topic (e.g. `mcp-resources`, `mcp-tools`, `mcp-prompts`). Returns reference documentation.
   - **`ServerInfo`** — no parameters needed. Returns server name, version, runtime, and timestamp.
   - **`Snippet`** — enter a snippet name. Requires a matching blob in storage (use the `save_snippet` tool from [FunctionsMcpTool](../FunctionsMcpTool/) to create one first).
5. The resource content is attached to the conversation as context for the model.

`GetDocumentation` hardcodes content in a dictionary so the sample works without any setup, but for production you'd store the content externally (blobs, a database, files) and read it the way `GetSnippetResource` does with `[BlobInput]`.

**Expected behavior:** Unlike tools and prompts, resources are not executed — they are read and attached as context. For example, selecting **ServerInfo** attaches JSON like `{"Name":"FunctionsMcpResources","Version":"1.4.0","Runtime":".NET 10.0.7","Timestamp":"2026-05-07T20:17:03Z"}` to the chat. You can then ask the model questions that reference this data.

### Redeploy and clean up

- **Redeploy:** `azd deploy`
- **Clean up all resources:** `azd down`

### Other auth options 

#### Key-based access

1. Set the auth level to `system` in `FunctionsMcpResources/host.json`:

    ```json
    "extensions": {
        "mcp": {
            "system": {
                "webhookAuthorizationLevel": "system"
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
| Connection refused locally | Ensure Azurite is running (`docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`) |
| API version not supported by Azurite | Pull the latest image (`docker pull mcr.microsoft.com/azure-storage/azurite`) and restart |
| `azd up` provision succeeded but deploy immediately failed: `unable to find a resource tagged with 'azd-service-name: mcp'` | The tag was provisioned but not propagated yet when `azd deploy` looked it up — run `azd deploy` again |
| `azd deploy` fails with Kudu restart error: `deployment was partially successful: [KuduSpecializer] Kudu has been restarted after package deployed` | Transient error — run `azd deploy` again |

## Examining the code

Resources are defined in `ResourceTemplateSamples.cs`. Each resource is a C# method with a `[Function]` attribute and an `[McpResourceTrigger]` binding:

```csharp
[Function(nameof(GetSnippetResource))]
public string? GetSnippetResource(
    [McpResourceTrigger(
        SnippetResourceTemplateUri,
        SnippetResourceName,
        Description = SnippetResourceDescription,
        MimeType = "application/json")]
        ResourceInvocationContext context,
    [BlobInput("snippets/{Name}.json")]
        string? snippetContent)
{
    return snippetContent;
}
```

URI template parameters from `[McpResourceTrigger]` (here, `Name` from `snippet://{Name}`) are flattened directly into the binding data, so `{Name}` in the `[BlobInput]` expression automatically resolves to the value supplied by the client.

## Next Steps

+ Learn more about the [Azure Functions MCP extension](https://learn.microsoft.com/azure/azure-functions/functions-bindings-mcp?pivots=programming-language-typescript)
+ Connect your MCP server to [Foundry agents](https://learn.microsoft.com/azure/azure-functions/functions-mcp-foundry-tools?tabs=oauth-id%2Cmcp-extension%2Cfoundry)
+ Add [API Management](https://github.com/Azure-Samples/remote-mcp-apim-functions-python) to your MCP server
