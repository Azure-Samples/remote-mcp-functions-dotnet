namespace FunctionsMcpResources;

internal sealed class ResourcesInformation
{
    // Resource templates
    public const string SnippetResourceTemplateUri = "snippet://{Name}";
    public const string SnippetResourceName = "Snippet";
    public const string SnippetResourceDescription =
        "Reads a code snippet by name from blob storage.";
    public const string ServerInfoResourceUri = "info://server";
    public const string ServerInfoResourceName = "ServerInfo";
    public const string ServerInfoResourceDescription =
        "Returns information about the MCP server, including version and runtime.";
}
