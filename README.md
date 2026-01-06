<!--
---
name: Remote MCP with Azure Functions (.NET/C#)
description: Run a remote MCP server on Azure functions.  
page_type: sample
languages:
- csharp
- bicep
- azdeveloper
products:
- azure-functions
- azure
urlFragment: remote-mcp-functions-dotnet
---
-->

# Getting Started with Remote MCP Servers using Azure Functions (.NET/C#)

This is a quickstart template to easily build and deploy a custom remote MCP server to the cloud using Azure functions. You can clone/restore/run on your local machine with debugging, and `azd up` to have it in the cloud in a couple minutes. 

The MCP server is configured with [built-in authentication](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization) using Microsoft Entra as the identity provider. The server also demonstrates how call a downstream service on behalf of the signed-in user in one of the tools. 

You can also use [API Management](https://learn.microsoft.com/azure/api-management/secure-mcp-servers) to secure the server, as well as network isolation using VNET.  

**Watch the video overview**

<a href="https://www.youtube.com/watch?v=XwnEtZxaokg">
  <img src="./images/video-overview.png" alt="Watch the video" width="500" />
</a>

If you're looking for this sample in more languages check out the [Node.js/TypeScript](https://github.com/Azure-Samples/remote-mcp-functions-typescript) and [Python](https://github.com/Azure-Samples/remote-mcp-functions-python) samples.  

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/Azure-Samples/remote-mcp-functions-dotnet)

## Prerequisites

### Required for all development approaches:
+ [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
+ [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.0.7030`
+ [Azure Developer CLI](https://aka.ms/azd) (for deployment)

### For Visual Studio development:
+ [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
+ Make sure to select the **Azure development** workload during installation

### For Visual Studio Code development:
+ [Visual Studio Code](https://code.visualstudio.com/)
+ [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)

> **Choose one**: You can use either Visual Studio OR Visual Studio Code. Both provide full debugging support, but the setup steps differ slightly.

Below is the architecture diagram for the Remote MCP Server using Azure Functions:

![Architecture Diagram](architecture-diagram.png)

## Prepare your local environment

1. An Azure Storage Emulator is needed for this particular sample because we will save and get snippets from blob storage. Start Azurite emulator: 

    ```shell
    docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
        mcr.microsoft.com/azure-storage/azurite
    ```

>**Note** if you use Azurite coming from VS Code extension you need to run `Azurite: Start` now or you will see errors.

1. (Optional) Update the `local.settings.json` file in the `src` folder to include your Microsoft Entra tenant ID. This helps the application correctly access your developer identity (required by one of the tools), even when you sign into multiple tenants.

    1. Obtain the tenant ID from the Azure CLI:

        ```cli
        az account show --query tenantId -o tsv
        ```

    1. Update `local.settings.json` to include the tenant ID:

        ```json
        {
          "IsEncrypted": false,
          "Values": {
            "AzureWebJobsStorage": "UseDevelopmentStorage=true",
            "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
            "AZURE_TENANT_ID": "<your-tenant-id>"
          }
        }
        ```

## Run your MCP Server locally

Choose your preferred development environment:

### Option A: Using Visual Studio Code

#### Run from Terminal
1. From the `src` folder, run this command to start the Functions host locally:

    ```shell
    cd src
    func start
    ```

## Connect to your *local* MCP server from MCP client tools

Once your Azure Functions MCP server is running locally (via either Visual Studio Code or Visual Studio), you can connect to it from various MCP client tools:

### Using VS Code with GitHub Copilot

1. Open **.vscode/mcp.json**. Find the server called _local-mcp-function_ and click **Start** above the name. The server is already set up with the running Function app's MCP endpoint:
    ```shell
    http://localhost:7071/runtime/webhooks/mcp
    ```
    > **Note**: If you're running from Visual Studio (not VS Code), use `http://localhost:7071/runtime/webhooks/mcp/sse` instead.

1. In Copilot chat **agent** mode enter a prompt to trigger the tool, e.g., select some code and enter this prompt

    ```plaintext
    Save this snippet as snippet1 
    ```

    ```plaintext
    Retrieve snippet1 and apply to NewFile.cs
    ```

1. When prompted to run the tool, consent by clicking **Continue**

1. The *hello_tool* demonstrates how to call Microsoft Graph on behalf of the signed-in user. When running locally, the MCP server will use your developer credentials (from Azure CLI or Visual Studio Code) for outbound calls to Microsoft Graph instead of an authorized user's identity. Invoke it via this prompt: 

    ```plaintext
    Greet with #hello_tool
    ```
    
1. When you're done, press Ctrl+C in the terminal window to stop the `func.exe` host process (or stop debugging in your IDE).

### Using MCP Inspector

1. In a **new terminal window**, install and run MCP Inspector

    ```shell
    npx @modelcontextprotocol/inspector node build/index.js
    ```

1. CTRL click to load the MCP Inspector web app from the URL displayed by the app (e.g. http://0.0.0.0:5173>/#resources)
1. Set the transport type to `Streamable HTTP` 
1. Set the URL to your running Function app's MCP endpoint and **Connect**:
    ```shell
    http://0.0.0.0:7071/runtime/webhooks/mcp
    ```
    > **Note**: If you're running from Visual Studio (not VS Code), use `http://localhost:7071/runtime/webhooks/mcp/sse` instead.

1. **List Tools**.  Click on a tool and **Run Tool**.

### Troubleshooting Local Development

**Problem**: Connection refused when trying to connect to MCP server
- **Solution**: Ensure Azurite is running (`docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`)

**Problem**: Wrong URL (0.0.0.0 vs localhost)
- **Solution**: Use `http://localhost:7071/runtime/webhooks/mcp` for VS Code, `http://localhost:7071/runtime/webhooks/mcp/sse` for Visual Studio, and `http://0.0.0.0:7071/runtime/webhooks/mcp` for MCP Inspector

**Problem**: Visual Studio F5 doesn't work
- **Solution**: Ensure Azure development workload is installed and `FunctionsMcpTool` is set as startup project  

**Problem**: The API version 2025-07-05 is not supported by Azurite
- **Solution**: Pull the latest Azurite image (`docker pull mcr.microsoft.com/azure-storage/azurite`) then restart Azurite and the app. 

## Verify local blob storage in Azurite

After testing the snippet save functionality locally, you can verify that blobs are being stored correctly in your local Azurite storage emulator.

### Using Azure Storage Explorer

1. Open Azure Storage Explorer
1. In the left panel, expand **Emulator & Attached** → **Storage Accounts** → **(Emulator - Default Ports) (Key)**
1. Navigate to **Blob Containers** → **snippets**
1. You should see any saved snippets as blob files in this container
1. Double-click on any blob to view its contents and verify the snippet data was saved correctly

### Using Azure CLI (Alternative)

If you prefer using the command line, you can also verify blobs using Azure CLI with the storage emulator:

```shell
# List blobs in the snippets container
az storage blob list --container-name snippets --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```

```shell
# Download a specific blob to view its contents
az storage blob download --container-name snippets --name <blob-name> --file <local-file-path> --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```

This verification step ensures your MCP server is correctly interacting with the local storage emulator and that the blob storage functionality is working as expected before deploying to Azure.

## Deploy to Azure for Remote MCP
Stop the local server with `Cntrl+C`. 

Sign in to Azure and initialize [azd](https://aka.ms/azd):

```shell
az login
azd auth login
```

Create a new azd project environment:

```shell
azd env new <environment-name>
```

Configure Visual Studio Code as an allowed client application so it can request access tokens from Microsoft Entra:

```shell
azd env set PRE_AUTHORIZED_CLIENT_IDS aebc6443-996d-45c2-90f0-388ff96faa56
```

Run this [azd](https://aka.ms/azd) command to provision the function app, with any required Azure resources, and deploy your code:

```shell
azd up
```

You can opt-in to a VNet being used in the sample. To do so, do this before `azd up`

```bash
azd env set VNET_ENABLED true
```

### Connect to remote MCP server in VS Code - GitHub Copilot

Connect to the remote MCP server after development finishes. For GitHub Copilot within VS Code, use `https://<funcappname>.azurewebsites.net/runtime/webhooks/mcp` for the URL. Note [mcp.json](.vscode/mcp.json) has already been included in this repo and will be picked up by VS Code, so just click **Start** above _remote-mcp-function_ to be prompted for `functionapp-name` (in your azd command output or /.azure/*/.env file). 

>[!TIP]
>Successful connect shows the number of tools the server has. You can see more details on the interactions between VS Code and server by clicking on **More... -> Show Output** above the server name. 

```json
{
    "inputs": [
        {
            "type": "promptString",
            "id": "functionapp-name",
            "description": "Azure Functions App Name"
        }
    ],
    "servers": {
        "remote-mcp-function": {
            "type": "http",
            "url": "https://${input:functionapp-name}.azurewebsites.net/runtime/webhooks/mcp",
        },
        "local-mcp-function": {
            "type": "http",
            "url": "http://0.0.0.0:7071/runtime/webhooks/mcp"
        }
    }
}
```

### Test remote MCP server 

You can test the save and get snippet tools as before during local development. Testing the *hello_tool* is a little different when the server is running remotely. Because the tool requires accessing the Microsoft Graph API, you need to provide consent for such access. When you invoke the tool for the first time, GitHub Copilot should return an error with instructions on how to provide the consent. Specifically, it should tell you to navigate to the `/.auth/login/aad` endpoint of your deployed function app. For example, if your function app is at `https://my-mcp-function-app.azurewebsites.net`, navigate to `https://my-mcp-function-app.azurewebsites.net/.auth/login/aad`. Try the tool again after consenting.

## Connect to your *remote* MCP server function app in other clients

Other clients like MCP Inspector are not yet recognized by Entra. This means you can't connect to the MCP server in these clients when built-in auth is configured. You can, however, connect with the system key. To do that:

1. Change the `webhookAuthorizationLevel` in [host.json](./src/host.json) from `Anonymous` to `System`, which makes accessing the webhook endpoint require a key. Previously with built-in auth configured, we didn't need a key because Entra helps authenticate the user. 

1. Disable built-in auth on the server app:
    ```shell
    az config set extension.dynamic_install_allow_preview=true && az webapp auth update --resource-group <resource_group> --name <function_app_name> --enabled false
    ```

1. Configure the deployment to skip built-in auth configuration:
    ```shell
    azd env set SKIP_BUILTIN_MCP_AUTH true
    ```

1. Redeploy the server with `azd up`. 

1. Connect to the MCP server by including the key in the URL: 
    ```plaintext
    https://<funcappname>.azurewebsites.net/runtime/webhooks/mcp?code=<your-mcp-extension-system-key>
    ```

The key can be obtained from the [portal](https://learn.microsoft.com/en-us/azure/azure-functions/function-keys-how-to?tabs=azure-portal). Go to the Function App resource, on the left menu look for **Functions -> App keys**. Obtain the system key named `mcp_extension`. 

Or use the CLI (`az rest --method post --uri "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/<resource_group>/providers/Microsoft.Web/sites/<function_app_name>/host/default/listkeys?api-version=2022-03-01" --query "systemKeys.mcp_extension" -o tsv`). 

## Redeploy your code

You can run the `azd up` command as many times as you need to both provision your Azure resources and deploy code updates to your function app.

>[!NOTE]
>Deployed code files are always overwritten by the latest deployment package.

## Clean up resources

When you're done working with your function app and related resources, you can use this command to delete the function app and its related resources from Azure and avoid incurring any further costs:

```shell
azd down
```

## Source Code

The function code for the `GetSnippet` and `SaveSnippet` endpoints are defined in [`SnippetsTool.cs`](./src/SnippetsTool.cs). The `McpToolsTrigger` attribute applied to the async `Run` method exposes the code function as an MCP Server.

The following shows the code for a few MCP server examples (get string, get object, save object). The *SayHello* tool [exchanges](./src/McpOutboundCredential/AppServiceAuthenticationOnBehalfOfCredential.cs) the berear token from server authentication with one that can access Microsoft Graph. See [McpOutboundCredential](./src/McpOutboundCredential/).

```csharp
[Function(nameof(GetSnippet))]
public object GetSnippet(
    [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)] ToolInvocationContext context,
    [BlobInput(BlobPath)] string snippetContent)
{
    return snippetContent;
}

[Function(nameof(SaveSnippet))]
[BlobOutput(BlobPath)]
public string SaveSnippet(
    [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)] ToolInvocationContext context,
    [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)] string name,
    [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)] string snippet)
{
    return snippet;
}

[Function(nameof(SayHello))]
public async Task<string> SayHello(
    [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
)
{
    logger.LogInformation("C# MCP tool trigger function processed a request.");

    var token = credentialProvider.GetTokenCredential().GetToken(new Azure.Core.TokenRequestContext(graphDefaultScopes), CancellationToken.None);

    using var graphClient = new GraphServiceClient(credentialProvider.GetTokenCredential(), graphDefaultScopes);

    try
    {
        var me = await graphClient.Me.GetAsync();
        return $"Hello, {me!.DisplayName} ({me?.UserPrincipalName})!";
    }
    catch (Exception ex)
    {
        // Code that handles exception
    }
}
```

## Next Steps

- Add [API Management](https://github.com/Azure-Samples/remote-mcp-apim-functions-python) to your MCP server
- Add [built-in auth](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization) to your MCP server
- Enable VNET using VNET_ENABLED=true flag
- Learn more about [related MCP efforts from Microsoft](https://github.com/microsoft/mcp/tree/main/Resources)
