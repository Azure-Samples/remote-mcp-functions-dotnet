# FunctionsMcpResources — MCP Resource Templates on Azure Functions (.NET/C#)

This project is a .NET 10 Azure Function app that exposes MCP (Model Context Protocol) resource templates as a remote MCP server. Resource templates allow MCP clients to discover and read structured data through URI-based patterns.

> **Note:** MCP tools are in the [FunctionsMcpTool](../FunctionsMcpTool/) project, and prompts are in the [FunctionsMcpPrompts](../FunctionsMcpPrompts/) project.

## Resources included

| Resource | URI | Description |
|----------|-----|-------------|
| `Snippet` | `snippet://{Name}` | Resource template that reads a code snippet by name from blob storage. Clients discover it via `resources/templates/list` and substitute the `Name` parameter. |
| `ServerInfo` | `info://server` | Static resource that returns server name, version, runtime, and timestamp. |

## Key concepts

- **Resource templates** have URI parameters (e.g., `{Name}`) that clients substitute at runtime — they're like parameterized endpoints.
- **Static resources** have fixed URIs and return the same structure every call.
- Resource metadata (like cache TTL) is configured in `Program.cs` via `ConfigureMcpResource`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local?pivots=programming-language-csharp#install-the-azure-functions-core-tools) >= `4.5.0`
- [Docker](https://www.docker.com/) (for the Azurite storage emulator — needed by the snippet resource template)

## Run locally

Start Azurite (required for the snippet resource which uses blob storage):

```shell
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 \
    mcr.microsoft.com/azure-storage/azurite
```

From this directory (`src/FunctionsMcpResources`), start the Functions host:

```shell
func start --port 7072
```

The MCP endpoint will be available at `http://localhost:7072/runtime/webhooks/mcp`.

## Deploy to Azure

```shell
azd env set DEPLOY_SERVICE resources
azd provision
azd deploy --service resources
```

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
    [BlobInput("snippets/{mcpresourceargs.Name}.json")]
        string? snippetContent)
{
    return snippetContent;
}
```

The `{mcpresourceargs.Name}` binding expression automatically extracts the `Name` parameter from the resource URI and passes it to the blob input binding.
