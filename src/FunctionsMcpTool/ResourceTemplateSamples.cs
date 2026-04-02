using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

public class ResourceTemplateSamples(ILogger<ResourceTemplateSamples> logger)
{
    /// <summary>
    /// Resource template that exposes snippets by name.
    /// The {Name} parameter in the URI makes this a resource template rather than
    /// a static resource — clients can discover it via resources/templates/list and
    /// read specific snippets by substituting the Name parameter.
    /// </summary>
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
        logger.LogInformation("Snippet resource template invoked: {Uri}", context.Uri);
        return snippetContent;
    }

    /// <summary>
    /// Static resource (no URI parameters) that returns server information.
    /// Demonstrates the difference between a static resource and a resource template.
    /// </summary>
    [Function(nameof(GetServerInfo))]
    public string GetServerInfo(
        [McpResourceTrigger(
            ServerInfoResourceUri,
            ServerInfoResourceName,
            Description = ServerInfoResourceDescription,
            MimeType = "application/json")]
            ResourceInvocationContext context)
    {
        logger.LogInformation("Server info resource invoked.");

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            Name = "FunctionsMcpTool",
            Version = "1.4.0",
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Timestamp = DateTime.UtcNow
        });
    }
}
