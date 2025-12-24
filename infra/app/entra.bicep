extension microsoftGraphV1

// Required parameters
@description('The unique name for the application registration (used for idempotency)')
param appUniqueName string

@description('The display name for the application registration')
param appDisplayName string

// Optional parameters with defaults
@description('References application or service contact information from a Service or Asset Management database')
param serviceManagementReference string = ''

@description('The default OAuth2 permission scope value')
param defaultScopeValue string = 'user_impersonation'

@description('The default OAuth2 permission scope display name')
param defaultScopeDisplayName string = 'Access application as user'

@description('The default OAuth2 permission scope description')
param defaultScopeDescription string = 'Allow the application to access the API on behalf of the signed-in user'

@description('Custom OAuth2 permission scopes to expose (optional)')
param customScopes array = []

@description('Client application IDs to pre-authorize for the default scope')
param preAuthorizedClientIds array = []

@description('Enable the application to accept tokens from both single and multi-tenant')
param signInAudience string = 'AzureADMyOrg'

@description('The application identifier URI (will be auto-generated if not provided)')
param identifierUri string = ''

@description('Redirect URIs for the application (e.g., for authentication callbacks)')
param redirectUris array = []

@description('Function App hostname for generating App Service Authentication redirect URI (optional)')
param functionAppHostname string = ''

@description('The client ID of the user-assigned managed identity to create federated identity credential for (optional)')
param managedIdentityClientId string = ''

@description('The principal ID of the user-assigned managed identity to create federated identity credential for (optional)')
param managedIdentityPrincipalId string = ''

@description('Tags for resource organization in Azure DevOps (azd)')
param tags object = {}

// Generate a default scope ID that's deterministic for this app
var defaultScopeId = guid(appUniqueName, 'default-scope', defaultScopeValue)

// Combine default scope with custom scopes
var allScopes = union([
  {
    adminConsentDescription: defaultScopeDescription
    adminConsentDisplayName: defaultScopeDisplayName
    id: defaultScopeId
    isEnabled: true
    type: 'User'
    userConsentDescription: defaultScopeDescription
    userConsentDisplayName: defaultScopeDisplayName
    value: defaultScopeValue
  }
], customScopes)

// Generate pre-authorized applications configuration
var preAuthorizedApps = [for clientId in preAuthorizedClientIds: {
  appId: clientId
  delegatedPermissionIds: [defaultScopeId]
}]

// Convert tags object to tag strings array
var tagStrings = !empty(tags) ? map(items(tags), tag => '${tag.key}:${tag.value}') : []

// Generate App Service Authentication redirect URI if Function App hostname is provided
var authRedirectUri = !empty(functionAppHostname) ? 'https://${functionAppHostname}/.auth/login/aad/callback' : ''

// Combine App Service Authentication URI with any additional URIs provided
var allRedirectUris = !empty(authRedirectUri) ? union([authRedirectUri], redirectUris) : redirectUris

// Generate a deterministic identifier URI when none is provided
// This ensures we always have a proper identifier URI set in Entra
var defaultIdentifierUri = 'api://${appUniqueName}-${uniqueString(subscription().id, resourceGroup().id, appUniqueName)}'
var finalIdentifierUri = !empty(identifierUri) ? identifierUri : defaultIdentifierUri

// Create the application registration
resource appRegistration 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: appUniqueName
  displayName: appDisplayName
  serviceManagementReference: !empty(serviceManagementReference) ? serviceManagementReference : null
  signInAudience: signInAudience
  identifierUris: [finalIdentifierUri]
  tags: tagStrings
  api: {
    oauth2PermissionScopes: allScopes
    requestedAccessTokenVersion: 2
    preAuthorizedApplications: preAuthorizedApps
  }
  web: {
    redirectUris: allRedirectUris
    implicitGrantSettings: {
      enableAccessTokenIssuance: false
      enableIdTokenIssuance: true
    }
  }
  requiredResourceAccess: [
    {
      // Microsoft Graph permissions
      resourceAppId: '00000003-0000-0000-c000-000000000000'
      resourceAccess: [
        {
          // User.Read delegated permission
          id: 'e1fe6dd8-ba31-4d61-89e7-88639da4683d'
          type: 'Scope'
        }
      ]
    }
  ]
}

// Create the service principal for the application
resource appServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: appRegistration.appId
  tags: tagStrings
}

// Create federated identity credential for the user-assigned managed identity (if provided)
resource federatedIdentityCredential 'Microsoft.Graph/applications/federatedIdentityCredentials@v1.0' = if (!empty(managedIdentityClientId) && !empty(managedIdentityPrincipalId)) {
  name: '${appRegistration.uniqueName}/mcp-function-managed-identity'
  audiences: [
    'api://AzureADTokenExchange'
  ]
  issuer: '${environment().authentication.loginEndpoint}${tenant().tenantId}/v2.0'
  subject: managedIdentityPrincipalId
  description: 'Federated identity credential for MCP Function App managed identity'
}

// Outputs for use by other modules and azd
@description('The application (client) ID of the registered application')
output applicationId string = appRegistration.appId

@description('The object ID of the application registration')
output applicationObjectId string = appRegistration.id

@description('The object ID of the service principal')
output servicePrincipalId string = appServicePrincipal.id

@description('The identifier URI of the application - returns the actual URI that was set')
output identifierUri string = finalIdentifierUri

@description('All OAuth2 permission scopes exposed by the application')
output exposedScopes array = [for scope in allScopes: {
  id: scope.id
  value: scope.value
  adminConsentDescription: scope.adminConsentDescription
  adminConsentDisplayName: scope.adminConsentDisplayName
  userConsentDescription: scope.userConsentDescription
  userConsentDisplayName: scope.userConsentDisplayName
}]

@description('The federated identity credential name (if managed identity was provided)')
output federatedIdentityCredentialName string = (!empty(managedIdentityClientId) && !empty(managedIdentityPrincipalId)) ? 'mcp-function-managed-identity' : ''

@description('The configured redirect URIs')
output configuredRedirectUris array = allRedirectUris

@description('The App Service Authentication redirect URI')
output authRedirectUri string = authRedirectUri
