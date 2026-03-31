using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

public class HelloTool(ILogger<HelloTool> logger)
{
    [Function(nameof(SayHello))]
    public string SayHello(
        [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
    )
    {
        logger.LogInformation("C# MCP tool trigger function processed a request.");
        return "Hello I am MCP Tool!";
    }

    /// <summary>
    /// Tool properties for this function are defined in Program.cs using
    /// ConfigureMcpTool, rather than McpToolProperty input binding attributes.
    /// </summary>
    [Function(nameof(EchoMessage))]
    public string EchoMessage(
        [McpToolTrigger(EchoToolName, EchoToolDescription)] ToolInvocationContext context
    )
    {
        var message = context.Arguments?.GetValueOrDefault(EchoMessagePropertyName)?.ToString() ?? "(empty)";
        logger.LogInformation("Echoing message: {Message}", message);
        return message;
    }
}