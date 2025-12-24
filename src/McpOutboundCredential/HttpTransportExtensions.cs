using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace FunctionsSnippetTool.McpOutboundCredential;

internal static class HttpTransportExtensions
{
    public static bool TryGetAuthenticationInformation(this HttpTransport context, out string? tenantId, out string? userAssertion)
    {
        tenantId = null;
        userAssertion = null;

        string? accessToken = null;
        string? idToken = null;

        bool hasApplicationToken =
            context.Headers.TryGetValue("Authorization", out var authorizationHeader) && authorizationHeader.StartsWith("Bearer ")
            || context.Headers.TryGetValue("X-MS-TOKEN-AAD-ACCESS-TOKEN", out accessToken)
            || context.Headers.TryGetValue("X-MS-TOKEN-AAD-ID-TOKEN", out idToken);

        if (context.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var encodedPrincipal)
            && hasApplicationToken)
        {
            userAssertion = authorizationHeader?.Replace("Bearer ", string.Empty) ?? accessToken ?? idToken;
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
                using var doc = System.Text.Json.JsonDocument.Parse(decoded);
                if (doc.RootElement.TryGetProperty("claims", out var claimsArray) && claimsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var claim in claimsArray.EnumerateArray())
                    {
                        if (claim.TryGetProperty("typ", out var typProp) &&
                            (typProp.GetString() == "tid" || typProp.GetString() == "http://schemas.microsoft.com/identity/claims/tenantid"))
                        {
                            if (claim.TryGetProperty("val", out var valProp))
                            {
                                tenantId = valProp.GetString();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tenantId = null;
            }
            return tenantId != null;
        }

        return false;
    }
}
