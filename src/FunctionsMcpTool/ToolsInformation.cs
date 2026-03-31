namespace FunctionsMcpTool;

internal sealed class ToolsInformation
{
    public const string HelloToolName = "hello_tool";
    public const string HelloToolDescription =
        "Simple hello world MCP Tool that responds with a hello message.";
    public const string GetSnippetToolName = "get_snippet";
    public const string GetSnippetToolDescription =
        "Gets a code snippet from your snippet collection.";
    public const string GetSnippetWithMetadataToolName = "get_snippet_with_metadata";
    public const string GetSnippetWithMetadataToolDescription =
        "Gets a code snippet with structured metadata.";
    public const string SaveSnippetToolName = "save_snippet";
    public const string SaveSnippetToolDescription =
        "Saves a code snippet into your snippet collection.";
    public const string SnippetNamePropertyName = "Name";
    public const string SnippetNamePropertyDescription = "The name of the snippet.";
    public const string BatchSaveSnippetsToolName = "batch_save_snippets";
    public const string BatchSaveSnippetsToolDescription =
        "Saves multiple code snippets at once into your snippet collection.";
    public const string SnippetItemsPropertyName = "snippet_items";
    public const string SnippetItemsPropertyDescription =
        "Array of snippets to save, each as an object with a single property where the key is the snippet name and the value is the content. Example: [{\"hello\": \"console.log('hi')\"}, {\"bye\": \"console.log('bye')\"}]";
}
