using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

/// <summary>
/// Demonstrates the explicit <c>WithInputSchema</c> and <c>WithOutputSchema</c>
/// fluent APIs (added in Worker.Extensions.Mcp 1.5.0). The tool's inputs and the
/// shape of its structured output are declared in <c>Program.cs</c> rather than
/// inferred from <c>McpToolProperty</c> attributes or POCO bindings.
///
/// The function reads arguments from <see cref="ToolInvocationContext.Arguments"/>
/// and returns a <see cref="CallToolResult"/> whose <c>StructuredContent</c>
/// matches the advertised output schema.
/// </summary>
internal class SearchSnippetsTool(ILogger<SearchSnippetsTool> logger)
{
    private static BlobServiceClient GetBlobServiceClient() =>
        new(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

    [Function(nameof(SearchSnippets))]
    public async Task<CallToolResult> SearchSnippets(
        [McpToolTrigger(SearchSnippetsToolName, SearchSnippetsToolDescription)]
            ToolInvocationContext context
    )
    {
        var prefix = context.Arguments?.GetValueOrDefault(SearchPrefixPropertyName)?.ToString() ?? string.Empty;
        var limit = context.Arguments?.GetValueOrDefault(SearchLimitPropertyName) switch
        {
            null => 10,
            JsonElement { ValueKind: JsonValueKind.Number } e => e.GetInt32(),
            var v => int.TryParse(v.ToString(), out var parsed) ? parsed : 10
        };
        limit = Math.Clamp(limit, 1, 100);

        logger.LogInformation(
            "Searching snippets with prefix '{Prefix}' (limit {Limit})", prefix, limit);

        var container = GetBlobServiceClient().GetBlobContainerClient("snippets");
        await container.CreateIfNotExistsAsync();

        var matches = new List<string>();
        await foreach (var blob in container.GetBlobsAsync(prefix: prefix))
        {
            if (matches.Count >= limit)
            {
                break;
            }

            var name = blob.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? blob.Name[..^".json".Length]
                : blob.Name;
            matches.Add(name);
        }

        var structured = new JsonObject
        {
            ["query"] = prefix,
            ["count"] = matches.Count,
            ["results"] = new JsonArray(matches.Select(n => (JsonNode)JsonValue.Create(n)!).ToArray())
        };

        var summary = matches.Count == 0
            ? $"No snippets found matching '{prefix}'."
            : $"Found {matches.Count} snippet(s): {string.Join(", ", matches)}";

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = summary }],
            StructuredContent = structured
        };
    }
}
