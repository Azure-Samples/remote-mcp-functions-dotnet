using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

public class SnippetsTool(ILogger<SnippetsTool> logger)
{
    private const string BlobPath = "snippets/{mcptoolargs.Name}.json";

    private static BlobServiceClient GetBlobServiceClient() =>
        new(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

    [Function(nameof(GetSnippet))]
    public Snippet? GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, SnippetNamePropertyDescription, true)]
            string name,
        [BlobInput(BlobPath)] string? snippetContent
    )
    {
        if (snippetContent is null)
        {
            return null;
        }

        return new Snippet { Name = name, Content = snippetContent };
    }

    /// <summary>
    /// Demonstrates returning rich MCP SDK types from the .Sdk extension.
    /// Returns a CallToolResult with both content blocks (for backward compatibility)
    /// and structured content (for clients that support it).
    /// </summary>
    [Function(nameof(GetSnippetWithMetadata))]
    public CallToolResult GetSnippetWithMetadata(
        [McpToolTrigger(GetSnippetWithMetadataToolName, GetSnippetWithMetadataToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, SnippetNamePropertyDescription, true)]
            string name,
        [BlobInput(BlobPath)] string? snippetContent
    )
    {
        var metadata = new
        {
            Name = name,
            Found = snippetContent is not null,
            CharacterCount = snippetContent?.Length ?? 0,
            RetrievedAt = DateTime.UtcNow
        };

        var metadataJson = JsonSerializer.Serialize(metadata);

        return new CallToolResult
        {
            Content =
            [
                new TextContentBlock { Text = snippetContent ?? $"Snippet '{name}' not found." },
                new TextContentBlock { Text = metadataJson }
            ],
            StructuredContent = JsonNode.Parse(metadataJson)
        };
    }

    /// <summary>
    /// Demonstrates rich POCO binding on the trigger parameter. The Snippet class
    /// properties are auto-discovered as tool inputs via the .Sdk extension,
    /// eliminating the need for individual McpToolProperty attributes.
    /// </summary>
    [Function(nameof(SaveSnippet))]
    [BlobOutput(BlobPath)]
    public string SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
            Snippet snippet,
        ToolInvocationContext context
    )
    {
        logger.LogInformation("Saving snippet '{Name}' via tool '{ToolName}'", snippet.Name, context.Name);
        return snippet.Content ?? string.Empty;
    }

    [Function(nameof(BatchSaveSnippets))]
    public async Task<string> BatchSaveSnippets(
        [McpToolTrigger(BatchSaveSnippetsToolName, BatchSaveSnippetsToolDescription)] ToolInvocationContext context,         
        [McpToolProperty(SnippetItemsPropertyName, SnippetItemsPropertyDescription, true)]
            IEnumerable<Dictionary<string, object>> snippetItems
    )
    {
        var containerClient = GetBlobServiceClient().GetBlobContainerClient("snippets");
        await containerClient.CreateIfNotExistsAsync();

        var savedSnippets = new List<string>();

        foreach (var item in snippetItems)
        {
            foreach (var (name, content) in item)
            {
                var blobClient = containerClient.GetBlobClient($"{name}.json");
                await blobClient.UploadAsync(
                    BinaryData.FromString(content?.ToString() ?? string.Empty),
                    overwrite: true
                );
                savedSnippets.Add(name);
            }
        }

        return JsonSerializer.Serialize(new
        {
            message = $"Successfully saved {savedSnippets.Count} snippets",
            snippets = savedSnippets
        });
    }
}
