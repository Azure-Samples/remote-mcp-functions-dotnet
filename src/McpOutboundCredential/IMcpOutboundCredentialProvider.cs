using Azure.Core;

namespace FunctionsSnippetTool.McpOutboundCredential;

public interface IMcpOutboundCredentialProvider
{
    public TokenCredential GetTokenCredential();
}
