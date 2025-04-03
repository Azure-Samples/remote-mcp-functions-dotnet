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
urlFragment: remote-mcp-python-functions
---
-->

# Getting Started with Remote MCP Servers using Azure Functions (.NET/C#)

This is a quickstart template to easily build and deploy a custom remote MCP server to the cloud using Azure functions. You can clone/restore/run on your local machine with debugging, and `azd up` to have it in the cloud in a couple minutes.  The MCP server is secured by design using keys and HTTPs, and allows more options for OAuth using EasyAuth and/or API Management as well as network isolation using VNET.  

If you're looking for this sample in more languages check out the [Node.js/TypeScript](https://github.com/Azure-Samples/remote-mcp-functions-typescript) and [Python](https://github.com/Azure-Samples/remote-mcp-functions-python) samples.  

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://github.com/codespaces/new?hide_repo_select=true&ref=main&repo=836901178)

## Prerequisites

+ [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
+ [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools)
+ [Azure Developer CLI](https://aka.ms/azd)
+ To use Visual Studio to run and debug locally:
  + [Visual Studio 2022](https://visualstudio.microsoft.com/vs/).
  + Make sure to select the **Azure development** workload during installation.
+ To use Visual Studio Code to run and debug locally:
  + [Visual Studio Code](https://code.visualstudio.com/)
  + [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)

Below is the architecture diagram for the Remote MCP Server using Azure Functions:

![Architecture Diagram](architecture-diagram.png)

## Prepare your local environment

An Azure Storage Emulator is needed for this particular sample because we will save and get snippets from blob storage. 

1. Start Azurite

    ```shell
    docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
        mcr.microsoft.com/azure-storage/azurite
    ```

>**Note** if you use Azurite coming from VS Code extension you need to run `Azurite: Start` now or you will see errors.

## Run your MCP Server locally from the terminal

1. From the `src` folder, run this command to start the Functions host locally:

    ```shell
    cd src
    func start
    ```

Note by default this will use the webhooks route: `/runtime/webhooks/mcp/sse`.  Later we will use this in Azure to set the key on client/host calls: `/runtime/webhooks/mcp/sse?code=<system_key>`

## Use the MCP server from within a client/host

### VS Code - Copilot Edits

1. **Add MCP Server** from command palette and add URL to your running Function app's SSE endpoint:
    ```shell
    http://0.0.0.0:7071/runtime/webhooks/mcp/sse
    ```
1. **List MCP Servers** from command palette and start the server
1. In Copilot chat agent mode enter a prompt to trigger the tool, e.g., select some code and enter this prompt

    ```plaintext
    Say Hello 
    ```

    ```plaintext
    Save this snippet as snippet1 
    ```

    ```plaintext
    Retrieve snippet1 and apply to NewFile.cs
    ```
1. When prompted to run the tool, consent by clicking **Continue**

1. When you're done, press Ctrl+C in the terminal window to stop the `func.exe` host process.

### MCP Inspector

1. In a **new terminal window**, install and run MCP Inspector

    ```shell
    npx @modelcontextprotocol/inspector node build/index.js
    ```

1. CTRL click to load the MCP Inspector web app from the URL displayed by the app (e.g. http://0.0.0.0:5173/#resources)
1. Set the transport type to `SSE` 
1. Set the URL to your running Function app's SSE endpoint and **Connect**:
    ```shell
    http://0.0.0.0:7071/runtime/webhooks/mcp/sse
    ```
1. **List Tools**.  Click on a tool and **Run Tool**.  

## Deploy to Azure for Remote MCP

Run this [azd](https://aka.ms/azd) command to provision the function app, with any required Azure resources, and deploy your code:

```shell
azd up
```
>**Using key based auth**
> This function requires a system key by default which can be obtained from the [portal](https://learn.microsoft.com/en-us/azure/azure-functions/function-keys-how-to?tabs=azure-portal), and then update the URL in your host/client to be:
> `https://<funcappname>.azurewebsites.net/runtime/webhooks/mcp/sse?code=<systemkey_for_mcp_extension>`
> via command line you can call `az functionapp keys list --resource-group <resource_group> --name <function_app_name>`
> Additionally, [API Management]() can be used for improved security and policies over your MCP Server, and [EasyAuth](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization) can be used to set up your favorite OAuth provider including Entra.  

You can opt-in to a VNet being used in the sample. To do so, do this before `azd up`

```bash
azd env set VNET_ENABLED true
```
After publish completes successfully, `azd` provides you with the URL endpoints of your new functions, but without the function key values required to access the endpoints. To obtain these same endpoints along with the **required function keys**, see [Invoke the function on Azure](https://learn.microsoft.com/azure/azure-functions/create-first-function-azure-developer-cli?pivots=programming-language-dotnet#invoke-the-function-on-azure)

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

The function code for the `GetSnippet` and `SaveSnippet` endpoints are defined in [`SnippetsTool.cs`](./dotnet/). The `McpToolsTrigger` attribute applied to the async `Run` method exposes the code function as an MCP Server.

This shows the code for a few MCP server examples (get string, get object, save object):  

```csharp
[Function(nameof(SayHello))]
public string SayHello(
    [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
)
{
    logger.LogInformation("Saying hello");
    return "Hello I am MCP Tool!";
}

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
```

## Next Steps

- Add [API Management]() to your MCP server
- Add [EasyAuth]() to your MCP server
- Enable VNET using VNET_ENABLED=true flag
- Learn more about [related MCP efforts from Microsoft]()
