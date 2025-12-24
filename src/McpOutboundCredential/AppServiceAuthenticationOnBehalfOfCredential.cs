using Azure.Core;
using Azure.Identity;

namespace FunctionsSnippetTool.McpOutboundCredential;

internal class AppServiceAuthenticationOnBehalfOfCredential : TokenCredential
{
    // This is the default for global Azure. If using a sovereign cloud, this value may need to be adjusted. Set the corresponding value in the `TokenExchangeAudience` app setting.
    // - Entra ID Global cloud: api://AzureADTokenExchange
    // - Entra ID US Government: api://AzureADTokenExchangeUSGov
    // - Entra ID China operated by 21Vianet: api://AzureADTokenExchangeChina
    private static string TokenExchangeAudience = Environment.GetEnvironmentVariable("TokenExchangeAudience") ?? "api://AzureADTokenExchange";
    private static string PublicTokenExchangeScope = $"{TokenExchangeAudience}/.default"; 

    private static readonly string? clientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID");
    private static readonly string? federatedCredentialClientId = Environment.GetEnvironmentVariable("OVERRIDE_USE_MI_FIC_ASSERTION_CLIENTID");

    private OnBehalfOfCredential innerCredential;

    public AppServiceAuthenticationOnBehalfOfCredential(string tenantId, string userAssertion)
    {
        ManagedIdentityCredential _managedIdentityCredential = new(federatedCredentialClientId!);
        Func<CancellationToken, Task<String>> clientAssertionCallback = async (CancellationToken cancellationToken) =>
          (await _managedIdentityCredential.GetTokenAsync(new TokenRequestContext(new[] { PublicTokenExchangeScope }), cancellationToken)).Token;

        innerCredential = new OnBehalfOfCredential(tenantId!, clientId!, clientAssertionCallback, userAssertion);
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => innerCredential.GetToken(requestContext, cancellationToken);
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => innerCredential.GetTokenAsync(requestContext, cancellationToken);
}
