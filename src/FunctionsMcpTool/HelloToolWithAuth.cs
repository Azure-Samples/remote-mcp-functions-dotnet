using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using static FunctionsMcpTool.ToolsInformation;

namespace FunctionsMcpTool;

/// <summary>
/// An example of calling Microsoft Graph on behalf of the signed-in user.
///
/// In production (built-in auth or App Service Authentication is used), the user's token arrives via HTTP headers.
/// This function exchanges that token for a Graph token using the On-Behalf-Of (OBO) flow,
/// then calls Graph as that user.
///
/// In local development, there is no signed-in user, so we fall back to your local
/// developer identity (az cli, VS Code, etc.) which can also call Graph.
/// </summary>
public class HelloToolWithAuth(ILogger<HelloToolWithAuth> logger, IHostEnvironment hostEnvironment)
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    [Function(nameof(HelloToolWithAuth))]
    public async Task<string> Run(
        [McpToolTrigger(HelloToolWithAuthName, HelloToolWithAuthDescription)] ToolInvocationContext context)
    {
        logger.LogInformation("HelloTool invoked.");

        // -----------------------------------------------------------------
        // Step 1: Get a TokenCredential that represents the current user.
        // -----------------------------------------------------------------
        TokenCredential credential;

        if (hostEnvironment.IsDevelopment())
        {
            // Locally use whatever developer identity is signed in (az login, VS Code, etc.).
            credential = new ChainedTokenCredential(
                new AzureCliCredential(),
                new VisualStudioCodeCredential(),
                new VisualStudioCredential(),
                new AzureDeveloperCliCredential());
        }
        else
        {
            // In production, built-in auth has already authenticated
            // the user and placed their token in HTTP headers.
            // We need to extract the user's token and tenant, then exchange
            // them for a Graph token using the OBO flow.
            credential = BuildOnBehalfOfCredential(context);
        }

        // -----------------------------------------------------------------
        // Step 2: Call Microsoft Graph as the user.
        // -----------------------------------------------------------------
        using var graphClient = new GraphServiceClient(credential, GraphScopes);

        try
        {
            var me = await graphClient.Me.GetAsync().ConfigureAwait(false);
            return $"Hello, {me?.DisplayName} ({me?.Mail})!";
        }
        catch (ODataError ex)
        {
            logger.LogError(ex, "Graph API Error: {Code} - {Message}", ex.Error?.Code, ex.Error?.Message);
            throw new InvalidOperationException(
                $"Graph API error ({ex.Error?.Code}): {ex.Error?.Message}", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            logger.LogError(ex, "AuthenticationFailedException: {Message}", ex.Message);
            throw new InvalidOperationException(
                "Failed to authenticate with Microsoft Graph. Check your credentials and permissions.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error: {Type} - {Message}", ex.GetType().FullName, ex.Message);
            throw;
        }  
    }

    /// <summary>
    /// Builds an On-Behalf-Of credential from the built-in auth headers that's
    /// injected into every authenticated request.
    ///
    /// The OBO flow exchanges the user's token for a downstream token (e.g. Graph)
    /// using three pieces of information:
    ///   1. The user's bearer token (from the Authorization header)
    ///   2. The user's tenant ID (from the X-MS-CLIENT-PRINCIPAL header)
    ///   3. A client assertion proving this app's identity (via managed identity
    ///      with a federated credential for token exchange)
    /// </summary>
    private static TokenCredential BuildOnBehalfOfCredential(ToolInvocationContext context)
    {
        if (!context.TryGetHttpTransport(out var transport))
            throw new InvalidOperationException("No HTTP transport available. Is built-in auth (App Service Authentication) enabled?");

        var userToken = GetUserToken(transport!);
        var tenantId = GetTenantId(transport!); 
        var clientAssertionCallback = BuildClientAssertionCallback();

        string clientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID")
            ?? throw new InvalidOperationException("WEBSITE_AUTH_CLIENT_ID is not set.");

        return new OnBehalfOfCredential(tenantId, clientId, clientAssertionCallback, userToken);
    }

    /// <summary>
    /// Extracts the user's bearer token from the Authorization header.
    /// Built-in auth ensures this header is always present for authenticated requests.
    /// </summary>
    private static string GetUserToken(HttpTransport transport)
    {
        // Try token store first (X-MS-TOKEN-AAD-ACCESS-TOKEN)
        if (transport.Headers.TryGetValue("X-MS-TOKEN-AAD-ACCESS-TOKEN", out var authToken) 
            && !string.IsNullOrEmpty(authToken))  
        {  
            return authToken;  
        }  

        // Fallback to Authorization header (for MCP clients sending Bearer tokens directly)  
        if (transport.Headers.TryGetValue("Authorization", out var authHeader)  
            && authHeader.StartsWith("Bearer "))  
        {  
            return authHeader["Bearer ".Length..];  
        }  

        throw new InvalidOperationException("No access token found. Is built-in auth (App Service Authentication) enabled with token store?");  
    }

    /// <summary>
    /// Extracts the tenant ID from the X-MS-CLIENT-PRINCIPAL header that
    /// built-in auth injects.
    /// </summary>
    private static string GetTenantId(HttpTransport transport)
    {
        if (!transport.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var encodedPrincipal))
            throw new InvalidOperationException("X-MS-CLIENT-PRINCIPAL header is missing.");

        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
        using var doc = System.Text.Json.JsonDocument.Parse(decoded);

        if (doc.RootElement.TryGetProperty("claims", out var claims) && claims.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var claim in claims.EnumerateArray())
            {
                if (claim.TryGetProperty("typ", out var typ) &&
                    (typ.GetString() == "tid" || typ.GetString() == "http://schemas.microsoft.com/identity/claims/tenantid"))
                {
                    return claim.GetProperty("val").GetString()
                        ?? throw new InvalidOperationException("Tenant ID claim has a null value.");
                }
            }
        }

        throw new InvalidOperationException("Could not find tenant ID claim in X-MS-CLIENT-PRINCIPAL.");
    }

    /// <summary>
    /// Creates a callback that uses a managed identity to obtain a client assertion token.
    /// This proves the app's identity during the OBO token exchange, without needing a client secret.
    ///
    /// Required environment variables:
    ///   OVERRIDE_USE_MI_FIC_ASSERTION_CLIENTID – Client ID of the managed identity with a
    ///                                            federated credential for token exchange.
    ///   TokenExchangeAudience (optional)       – Defaults to "api://AzureADTokenExchange".
    /// </summary>
    private static Func<CancellationToken, Task<string>> BuildClientAssertionCallback()
    {
        string federatedMiClientId = Environment.GetEnvironmentVariable("OVERRIDE_USE_MI_FIC_ASSERTION_CLIENTID")
            ?? throw new InvalidOperationException("OVERRIDE_USE_MI_FIC_ASSERTION_CLIENTID is not set.");

        string tokenExchangeAudience = Environment.GetEnvironmentVariable("TokenExchangeAudience") ?? "api://AzureADTokenExchange";
        var managedIdentity = new ManagedIdentityCredential(federatedMiClientId);

        return async (cancellationToken) =>
            (await managedIdentity.GetTokenAsync(
                new TokenRequestContext([$"{tokenExchangeAudience}/.default"]),
                cancellationToken).ConfigureAwait(false)).Token;
    }
}

