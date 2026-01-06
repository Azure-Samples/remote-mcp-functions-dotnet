using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunctionsSnippetTool.McpOutboundCredential;

internal class McpOutboundCredentialMiddleware(IHostEnvironment hostEnvironment) : IFunctionsWorkerMiddleware
{

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var credentialProvider = context.InstanceServices.GetService<IMcpOutboundCredentialProvider>() as McpOutboundCredentialProvider;

        if (hostEnvironment.IsDevelopment())
        {
            // This sample application isn't going to do anything beyond User.Read. Therefore, we can just use the developer identity.
            // However, for operations needing broader permissions, you would need to define an app registration with those, to be used in the local context.
            // App Service Authentication and Authorization is not available locally, but you could use the same OnBehalfOf approach with a developer identity through that application.
            //
            // If you have multiple tenants available to your account, you may also need to ensure the `AZURE_TENANT_ID` environment variable is set to the appropriate target tenant ID.
            credentialProvider!.SetTokenCredential(new ChainedTokenCredential(
                new VisualStudioCredential(),
                new VisualStudioCodeCredential(),
                new AzureCliCredential(),
                new AzurePowerShellCredential(),
                new AzureDeveloperCliCredential()
            ));
        }
        else if (hostEnvironment.IsProduction() || hostEnvironment.IsStaging())
        {
            if (context.Items.TryGetValue("ToolInvocationContext", out var ticObj)
                && ((ticObj as ToolInvocationContext)?.TryGetHttpTransport(out var transport) ?? false)
                && (transport?.TryGetAuthenticationInformation(out string? tenantId, out string? userAssertion) ?? false)
                && tenantId is not null
                && userAssertion is not null)
            {
                credentialProvider!.SetTokenCredential(new AppServiceAuthenticationOnBehalfOfCredential(tenantId, userAssertion));
            }
            else
            {
                // throw new InvalidOperationException($"Transport using App Service Authentication and Authorization not available.");
                // When built-in auth is disabled, use Managed Identity
                credentialProvider!.SetTokenCredential(new ManagedIdentityCredential());
            }
        }
        else
        {
            throw new InvalidOperationException($"Unsupported environment. Supported environments are {Environments.Development}, {Environments.Staging}, and {Environments.Production}.");
        }

        await next(context);
    }

}
