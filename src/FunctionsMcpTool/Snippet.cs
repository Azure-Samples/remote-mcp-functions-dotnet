using System.ComponentModel;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace FunctionsMcpTool;

/// <summary>
/// Snippet model decorated with McpContent to enable structured content output.
/// When used as a trigger parameter, properties are auto-discovered as tool inputs.
/// When returned from a tool function, this is serialized as structured content.
/// </summary>
[McpContent]
public class Snippet
{
    [Description("The name of the snippet")]
    public required string Name { get; set; }

    [Description("The code snippet content")]
    public string? Content { get; set; }
}
