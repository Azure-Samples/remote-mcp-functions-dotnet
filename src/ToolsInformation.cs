namespace FunctionsSnippetTool;

internal sealed class ToolsInformation
{
    public const string SaveSnippetToolName = "save_snippet";
    public const string SaveSnippetToolDescription =
        "Saves a code snippet into your snippet collection.";
    public const string GetSnippetToolName = "get_snippets";
    public const string GetSnippetToolDescription =
        "Gets code snippets from your snippet collection.";
    public const string SnippetNamePropertyName = "snippetname";
    public const string SnippetPropertyName = "snippet";
    public const string SnippetNamePropertyDescription = "The name of the snippet.";
    public const string SnippetPropertyDescription = "The code snippet.";
    public const string PropertyType = "string";
    
    // Property types for MCP tools
    public const string StringPropertyType = "string";
    public const string ArrayPropertyType = "array";
    public const string ObjectPropertyType = "object";
    public const string NumberPropertyType = "number";
    public const string BooleanPropertyType = "boolean";
    
    public const string HelloToolName = "hello";
    public const string HelloToolDescription =
        "Simple hello world MCP Tool that responses with a hello message.";
}
