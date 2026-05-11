## [remote-mcp-functions-dotnet] Changelog

<a name="1.2.0"></a>
# 1.2.0

*Features*
* Updated Microsoft.Azure.Functions.Worker.Extensions.Mcp from 1.4.0 to 1.5.0 across all sample projects (FunctionsMcpTool, FunctionsMcpResources, FunctionsMcpPrompts, McpWeatherApp, FunctionsMcpApp)
* Added `search_snippets` tool sample in FunctionsMcpTool demonstrating the new `WithInputSchema` and `WithOutputSchema` fluent APIs for declaring explicit JSON schemas for tool inputs and structured outputs

*Bug Fixes*
* Fixed `[BlobInput]` binding expression in `FunctionsMcpResources.GetSnippetResource` to use `{Name}` instead of the non-existent `{mcpresourceargs.Name}` (URI template parameters from `[McpResourceTrigger]` are flattened directly into binding data)

<a name="1.1.0"></a>
# 1.1.0

*Features*
* Updated Microsoft.Azure.Functions.Worker.Extensions.Mcp from 1.3.0 to 1.4.0
* Added MCP prompt samples demonstrating `McpPromptTrigger`, `McpPromptArgument`, and fluent `ConfigureMcpPrompt` API
* Added MCP resource template sample demonstrating `McpResourceTrigger` with URI templates
* Added static MCP resource sample (`info://server`)
* Added fluent `ConfigureMcpResource` with `WithMetadata` example

*Breaking Changes*
* ...

<a name="1.0.1"></a>
# 1.0.1 (2024-12-28)

*Features*
* Updated Microsoft.Azure.Functions.Worker.Extensions.Mcp from 1.0.0-preview.5 to 1.0.0-preview.6

*Bug Fixes*
* ...

*Breaking Changes*
* ...
