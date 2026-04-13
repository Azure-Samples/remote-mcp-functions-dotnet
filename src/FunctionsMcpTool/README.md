# FunctionsMcpTool — Remote MCP Server on Azure Functions (.NET/C#)

This project is a .NET 10 Azure Function app that exposes multiple MCP (Model Context Protocol) tools as a remote MCP server. It includes tools for snippets, QR code generation, badges, echo, hello, and a **hello with auth** tool that demonstrates the On-Behalf-Of (OBO) flow to call Microsoft Graph as the signed-in user.

> **Note:** MCP prompts and resource templates are in separate projects — see [FunctionsMcpPrompts](../FunctionsMcpPrompts/) and [FunctionsMcpResources](../FunctionsMcpResources/).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.5.0`
- [Azure Developer CLI (azd)](https://aka.ms/azd) **1.23.x or above** (for deployment)
- [Docker](https://www.docker.com/) (for the Azurite storage emulator)

## Prepare your local environment

An Azure Storage Emulator is needed because the snippet tools save and retrieve blobs from storage. Start Azurite:

```shell
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
    mcr.microsoft.com/azure-storage/azurite
```

> If you use the Azurite VS Code extension instead, run **Azurite: Start** now.

## Run locally

From this directory (`src/FunctionsMcpTool`), start the Functions host:

```shell
func start
```

## Connect to the MCP server

### Option A: VS Code with GitHub Copilot

1. Open **`.vscode/mcp.json`** in the workspace root. Find the server called **`local-mcp-function`** and click **Start** above the name. It points to:

   ```
   http://localhost:7071/runtime/webhooks/mcp
   ```

2. In Copilot chat **agent** mode, try prompts like:

   ```
   Say Hello
   ```

   ```
   Save this snippet as snippet1
   ```

   ```
   Retrieve snippet1 and apply to NewFile.cs
   ```

3. When prompted to run a tool, consent by clicking **Continue**.

4. Press `Ctrl+C` in the terminal to stop the function host when done.

### Option B: MCP Inspector

1. In a new terminal, install and run MCP Inspector:

   ```shell
   npx @modelcontextprotocol/inspector
   ```

2. Open the Inspector URL (e.g. `http://0.0.0.0:5173/#resources`).
3. Set the transport type to **Streamable HTTP**.
4. Set the URL to `http://0.0.0.0:7071/runtime/webhooks/mcp` and click **Connect**.
5. Click **List Tools**, select a tool, and **Run Tool**.

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

Configure VS Code as an allowed client application for Microsoft Entra authentication:

```shell
azd env set PRE_AUTHORIZED_CLIENT_IDS aebc6443-996d-45c2-90f0-388ff96faa56
```

Set the deployment service to `tools` (this project):

```shell
azd env set DEPLOY_SERVICE tools
```

### Step 3: Provision and deploy

```shell
azd provision
```

When prompted, pick your subscription and an Azure region.

```shell
azd deploy --service tools
```

### Step 4: Consent to the application

The `HelloToolWithAuth` tool requires consent for delegated permission to access Microsoft Graph. For testing, you can grant consent just for yourself by logging into the application in a browser. See [Consent authoring](#consent-authoring) for how you would handle this for production scenarios.

Navigate to the `/.auth/login/aad` endpoint of your deployed function app. For example, if your function app is at `https://my-mcp-function-app.azurewebsites.net`, navigate to:

```
https://my-mcp-function-app.azurewebsites.net/.auth/login/aad
```

Sign in with your Azure subscription email and accept the permissions prompt. This completes the consent flow for you.

### Step 5: Connect to the remote MCP server

Open **`.vscode/mcp.json`** and click **Start** above **`remote-functions-mcp-tool`**. You'll be prompted for `functionapp-name` (find it in your azd command output or the `/.azure/<env>/.env` file). You'll also be prompted to authenticate with Microsoft—click **Allow** and sign in.

> **Tip:** A successful connection shows the number of tools the server exposes. Click **More... → Show Output** above the server name to see request/response details.

## Redeploy and clean up

- **Redeploy:** `azd deploy --service tools`
- **Clean up all resources:** `azd down`

## Examining the code

### MCP tool basics

Each tool is a C# method with a `[Function]` attribute and an `[McpToolTrigger]` binding that exposes it as an MCP tool. For example, the snippet tools in `SnippetsTool.cs` use `[BlobInput]` and `[BlobOutput]` bindings to read/write Azure Blob Storage directly:

```csharp
[Function(nameof(GetSnippet))]
public Snippet? GetSnippet(
    [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
        ToolInvocationContext context,
    [McpToolProperty(SnippetNamePropertyName, SnippetNamePropertyDescription, true)]
        string name,
    [BlobInput(BlobPath)] string? snippetContent)
{
    // ...
}

[Function(nameof(SaveSnippet))]
[BlobOutput(BlobPath)]
public string SaveSnippet(
    [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
        Snippet snippet,
    // ...
)
{
    // ...
}
```

### Calling Microsoft Graph with the On-Behalf-Of flow (`HelloToolWithAuth`)

The `HelloToolWithAuth` tool demonstrates how to call a downstream API (Microsoft Graph) **as the signed-in user** using the On-Behalf-Of (OBO) flow.

**Local development** falls back to your local developer identity (Azure CLI, VS Code, etc.):

```csharp
if (hostEnvironment.IsDevelopment())
{
    credential = new ChainedTokenCredential(
        new AzureCliCredential(),
        new VisualStudioCodeCredential(),
        new VisualStudioCredential(),
        new AzureDeveloperCliCredential());
}
else
{
    credential = BuildOnBehalfOfCredential(context);
}
```

**In production**, the `BuildOnBehalfOfCredential` method exchanges the user's auth token for a Microsoft Graph token using three pieces of information:

1. **The user's bearer token** — extracted from the `X-MS-TOKEN-AAD-ACCESS-TOKEN` header (or `Authorization` fallback)
2. **The user's tenant ID** — decoded from the `X-MS-CLIENT-PRINCIPAL` header
3. **A client assertion** — obtained from a managed identity with a federated credential, proving the app's identity without a client secret

```csharp
private static TokenCredential BuildOnBehalfOfCredential(ToolInvocationContext context)
{
    if (!context.TryGetHttpTransport(out var transport))
        throw new InvalidOperationException(
            "No HTTP transport available. Is built-in auth (App Service Authentication) enabled?");

    var userToken = GetUserToken(transport!);
    var tenantId = GetTenantId(transport!);
    var clientAssertionCallback = BuildClientAssertionCallback();

    string clientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID")
        ?? throw new InvalidOperationException("WEBSITE_AUTH_CLIENT_ID is not set.");

    return new OnBehalfOfCredential(tenantId, clientId, clientAssertionCallback, userToken);
}
```

The resulting credential is then used to create a `GraphServiceClient` and call `Me.GetAsync()` to greet the user by name:

```csharp
using var graphClient = new GraphServiceClient(credential, GraphScopes);
var me = await graphClient.Me.GetAsync().ConfigureAwait(false);
return $"Hello, {me?.DisplayName} ({me?.Mail})!";
```

## Consent authoring

In the steps described for this example, you consented to the application by signing into it in a browser. This allowed the application to request delegated permissions to the Microsoft Graph. There are two main ways that consent can be handled:

- **User consent** — This is the approach used in the example above. Each user signs into the application and consents to the permissions requested. They can only do this for themselves, unless they are a tenant administrator with the ability to consent on behalf of others. In this sample, user consent is appropriate because it allows you to quickly test things without impacting other users. However, the way user consent is authored in this sample does not reflect how you would typically do it in a production scenario. This is described in more detail below.

- **Admin consent** — A tenant administrator can consent to the application on behalf of all users when they sign in and review the permissions. Once this is done, individual users can sign in without needing to consent themselves. This approach is more scalable and ensures that all users can access the application without running into consent issues. For the purposes of a sample, admin consent is not appropriate, but it is a great choice for production scenarios.

The user consent approach for this sample is a separate login because the sample uses Visual Studio Code as the client. Although Visual Studio Code is pre-authorized to our application, that only creates consent for the user to call the MCP server. It doesn't create consent for the MCP server to call the Microsoft Graph on behalf of the user. When we log into the application directly, we request Microsoft Graph permissions as part of a combined consent experience.

The main difference is that because Visual Studio Code is using a single sign-on flow, it only requests a token for the MCP server. It does not present an opportunity for the user to interactively consent to any permissions needed for or by the MCP server. If you built a client that used an interactive login of some kind, you could have it all handled entirely by that client. It would not be necessary to have a separate browser login.

See [Overview of permissions and consent in the Microsoft identity platform](https://learn.microsoft.com/en-us/entra/identity-platform/permissions-consent-overview) for additional information on how Entra ID handles consent.

## Tools included

| Tool | Description |
|------|-------------|
| `hello_tool` | Simple hello world tool |
| `hello_tool_with_auth` | Greets the signed-in user by name via Microsoft Graph (OBO flow) |
| `echo_message` | Echoes back a provided message (properties defined in `Program.cs`) |
| `save_snippet` | Saves a code snippet to blob storage |
| `get_snippet` | Retrieves a code snippet from blob storage |
| `get_snippet_with_metadata` | Retrieves a snippet with structured metadata |
| `batch_save_snippets` | Saves multiple snippets at once |
| `generate_qr_code` | Generates a QR code image from text |
| `generate_badge` | Generates an SVG status badge |
| `get_website_preview` | Fetches a website preview |

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Connection refused locally | Ensure Azurite is running (`docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite`) |
| API version not supported by Azurite | Pull the latest image (`docker pull mcr.microsoft.com/azure-storage/azurite`) and restart |
| `hello_tool_with_auth` fails locally | Ensure you're signed in with `az login` or the VS Code Azure account extension |
| OBO errors in production | Verify that consent has been granted (see Step 4) and that the Entra app registration is configured correctly |
