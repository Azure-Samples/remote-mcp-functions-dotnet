using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using static FunctionsSnippetTool.ToolsInformation;

namespace FunctionsSnippetTool;

public class SnippetsTool()
{
    private const string BlobPath = "snippets/{mcptoolargs." + SnippetNamePropertyName + "}.json";

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
}
