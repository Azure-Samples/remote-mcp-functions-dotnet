using FunctionsSnippetTool.McpOutboundCredential;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using static FunctionsSnippetTool.ToolsInformation;

namespace FunctionsSnippetTool;

public class HelloTool(ILogger<HelloTool> logger, IMcpOutboundCredentialProvider credentialProvider)
{
    private static readonly string[] graphDefaultScopes = ["https://graph.microsoft.com/.default"];

    [Function(nameof(SayHello))]
    public async Task<string> SayHello(
        [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
    )
    {
        logger.LogInformation("C# MCP tool trigger function processed a request.");

        var token = credentialProvider.GetTokenCredential().GetToken(new Azure.Core.TokenRequestContext(graphDefaultScopes), CancellationToken.None);

        using var graphClient = new GraphServiceClient(credentialProvider.GetTokenCredential(), graphDefaultScopes);

        try
        {
            var me = await graphClient.Me.GetAsync();
            return $"Hello, {me!.DisplayName} ({me?.UserPrincipalName})!";
        }
        catch (Exception ex)
        {
            var hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
            var errorOutput = new
            {
                authenticated = false,
                message = $"Error during token exchange and Graph API call: {ex.Message}. " +
                            (!string.IsNullOrEmpty(hostname) 
                                ? $"You're logged in but might need to grant consent to the application. Open a browser to the following link to consent: https://{hostname}/.auth/login/aad"
                                : "You might need to grant consent to the application."),
                error = ex.GetType().Name
            };

            return System.Text.Json.JsonSerializer.Serialize(errorOutput, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
