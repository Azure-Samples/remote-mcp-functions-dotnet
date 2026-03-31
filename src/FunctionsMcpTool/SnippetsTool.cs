using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

public class SnippetsTool
{
    private const string BlobPath = "snippets/{mcptoolargs." + SnippetNamePropertyName + "}.json";

    private static BlobServiceClient GetBlobServiceClient() =>
        new(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
            ToolInvocationContext context,
        [BlobInput(BlobPath)] string snippetContent
    )
    {
        return snippetContent;
    }

    [Function(nameof(SaveSnippet))]
    [BlobOutput(BlobPath)]
    public string SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, SnippetNamePropertyDescription, true)]
            string name,
        [McpToolProperty(SnippetPropertyName, SnippetPropertyDescription, true)]
            string snippet
    )
    {
        return snippet;
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
