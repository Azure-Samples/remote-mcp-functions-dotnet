# FunctionsMcpPrompts — MCP Prompts on Azure Functions (.NET/C#)

This project is a .NET 10 Azure Function app that exposes MCP (Model Context Protocol) prompts as a remote MCP server. Prompts are reusable prompt templates that MCP clients can discover and invoke, optionally with arguments.

> **Note:** MCP tools are in the [FunctionsMcpTool](../FunctionsMcpTool/) project, and resource templates are in the [FunctionsMcpResources](../FunctionsMcpResources/) project.

## Prompts included

| Prompt | Arguments | Description |
|--------|-----------|-------------|
| `code_review_checklist` | _(none)_ | Returns a structured code review checklist for evaluating code changes. |
| `summarize_content` | `topic` (required), `audience` (optional) | Generates a summarization prompt tailored to a given topic and audience. |
| `generate_documentation` | `function_name` (required), `style` (optional) | Generates API documentation for a function. Arguments are configured in `Program.cs`. |

## Key concepts

- **Simple prompts** (like `code_review_checklist`) take no arguments and return static prompt text.
- **Parameterized prompts** use `[McpPromptArgument]` input bindings to accept arguments from the client.
- **Program.cs-configured prompts** (like `generate_documentation`) define their arguments via `ConfigureMcpPrompt` in `Program.cs` instead of using attribute-based bindings, and read them from `PromptInvocationContext.Arguments`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.5.0`
- [Docker](https://www.docker.com/) (for the Azurite storage emulator)

## Prepare your local environment

An Azure Storage Emulator is needed for the Functions runtime. Start Azurite:

```shell
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
    mcr.microsoft.com/azure-storage/azurite
```

> If you use the Azurite VS Code extension instead, run **Azurite: Start** now.

## Run locally

From this directory (`src/FunctionsMcpPrompts`), start the Functions host:

```shell
func start 
```

The MCP endpoint will be available at `http://localhost:7071/runtime/webhooks/mcp`.

## Connect from VS Code 

Open **.vscode/mcp.json** in the workspace root. Find the server called **`local-mcp-function`** and click **Start** above the name. It points to:

```
http://localhost:7071/runtime/webhooks/mcp
```

Prompts appear as `/` slash commands in VS Code Chat. Type `/` and select a prompt (e.g. `/mcp.<server-name>.<prompt>`). 

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

> **Tip:** A successful connection shows the number of prompts the server exposes. Click **More... → Show Output** above the server name to see request/response details.

### Test your prompts

Once connected, prompts appear as `/` slash commands in VS Code Chat, prefixed with the server name:

1. Open the **Chat** panel.
2. Type `/` to see the available prompts. They appear as `/mcp.<server-name>.<prompt>` (e.g. `/mcp.remote-functions-mcp-oauth.summarize_content`).
3. Select a prompt. If it has arguments, VS Code will ask you to fill them in.
4. The returned prompt text is injected as context for the conversation.

For example, select `/mcp.remote-functions-mcp-oauth.summarize_content`, enter `Azure Functions` as the topic, and optionally choose an audience level (e.g. `beginner`, `developer`, `executive`). The server returns a tailored summarization prompt that the LLM uses to shape its response.

### Redeploy and clean up

- **Redeploy:** `azd deploy`
- **Clean up all resources:** `azd down`

### Other auth options

#### Key-based access

1. Set the auth level to `function` in `FunctionsMcpPrompts/host.json`:

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

## Examining the code

Prompts are defined in `PromptSamples.cs`. Each prompt is a C# method with a `[Function]` attribute and an `[McpPromptTrigger]` binding:

```csharp
[Function(nameof(SummarizeContent))]
public string SummarizeContent(
    [McpPromptTrigger(SummarizePromptName, Description = SummarizePromptDescription)]
        PromptInvocationContext context,
    [McpPromptArgument("topic", "The topic or content to summarize.", isRequired: true)]
        string topic,
    [McpPromptArgument("audience", "Target audience (e.g., 'executive', 'developer', 'beginner').")]
        string? audience)
{
    // Returns a formatted prompt string
}
```

The `[McpPromptArgument]` attributes define the arguments that MCP clients see when they list available prompts.

## Next Steps

+ Learn more about the [Azure Functions MCP extension](https://learn.microsoft.com/azure/azure-functions/functions-bindings-mcp?pivots=programming-language-typescript)
+ Connect your MCP server to [Foundry agents](https://learn.microsoft.com/azure/azure-functions/functions-mcp-foundry-tools?tabs=oauth-id%2Cmcp-extension%2Cfoundry)
+ Add [API Management](https://github.com/Azure-Samples/remote-mcp-apim-functions-python) to your MCP server
