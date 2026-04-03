using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace FunctionsMcpApp;

/// <summary>
/// MCP App tools that are configured with UI views in Program.cs using the fluent API.
/// Each tool function defines the logic; the app configuration (view, CSP, permissions)
/// is set up separately via <c>ConfigureMcpTool(...).AsMcpApp(...)</c>.
/// </summary>
public class AppTools(ILogger<AppTools> logger)
{
    /// <summary>
    /// A simple MCP App tool with a greeting view.
    /// The view is configured in Program.cs as a file-backed HTML page.
    /// </summary>
    [Function(nameof(HelloApp))]
    public string HelloApp(
        [McpToolTrigger("HelloApp", "A simple MCP App that displays a greeting.")] ToolInvocationContext context)
    {
        logger.LogInformation("HelloApp tool invoked.");
        return "Hello from MCP App!";
    }

    /// <summary>
    /// A dashboard MCP App with clipboard permissions and CSP configuration.
    /// Demonstrates the full fluent API including WithCsp, WithPermissions, and WithStaticAssets.
    /// </summary>
    [Function(nameof(SnippetDashboard))]
    public string SnippetDashboard(
        [McpToolTrigger("SnippetDashboard", "Opens a snippet dashboard with status information.")]
            ToolInvocationContext context)
    {
        logger.LogInformation("SnippetDashboard tool invoked.");
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            Status = "Online",
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// A standard MCP tool (not an app) for comparison.
    /// Shows that regular tools and MCP App tools coexist in the same project.
    /// </summary>
    [Function(nameof(GetServerTime))]
    public string GetServerTime(
        [McpToolTrigger("GetServerTime", "Returns the current server time in UTC.")]
            ToolInvocationContext context)
    {
        logger.LogInformation("GetServerTime tool invoked.");
        return DateTime.UtcNow.ToString("O");
    }
}
