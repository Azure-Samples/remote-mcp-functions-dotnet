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

## Run locally

From this directory (`src/FunctionsMcpPrompts`), start the Functions host:

```shell
func start --port 7073
```

The MCP endpoint will be available at `http://localhost:7073/runtime/webhooks/mcp`.

## Deploy to Azure

```shell
azd env set DEPLOY_SERVICE prompts
azd provision
azd deploy --service prompts
```

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
