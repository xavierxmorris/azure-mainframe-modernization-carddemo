@description('Short environment name used for tags and readable resource names.')
param environmentName string

@description('Azure region for all resources.')
param location string

@description('Stable unique token generated at subscription scope.')
@minLength(13)
@maxLength(13)
param resourceToken string

@description('Tags applied to all resources.')
param tags object

@description('Whether the web Container App already exists.')
param webExists bool

@description('Whether the web ingress is externally accessible.')
param externalIngress bool

var containerRegistryName = take(toLower('cr${replace(environmentName, '-', '')}${resourceToken}'), 50)
var containerAppsEnvironmentName = 'cae-${environmentName}-${take(resourceToken, 6)}'
var containerAppName = 'ca-${environmentName}-${take(resourceToken, 6)}'
var logAnalyticsName = 'log-${environmentName}-${take(resourceToken, 6)}'
var identityName = 'id-${environmentName}-${take(resourceToken, 6)}'
var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    dataEndpointEnabled: false
    networkRuleBypassOptions: 'AzureServices'
    policies: {
      exportPolicy: {
        status: 'enabled'
      }
      quarantinePolicy: {
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'disabled'
      }
      trustPolicy: {
        status: 'disabled'
        type: 'Notary'
      }
    }
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Disabled'
  }
}

resource workloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, workloadIdentity.id, acrPullRoleDefinitionId)
  scope: containerRegistry
  properties: {
    principalId: workloadIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: containerAppsEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

resource existingWeb 'Microsoft.App/containerApps@2026-01-01' existing = if (webExists) {
  name: containerAppName
}

var currentOrBootstrapImage = webExists
  ? existingWeb!.properties.template.containers[0].image
  : 'mcr.microsoft.com/azuredocs/containerapps-helloworld@sha256:e9b3e7c34664c7cffd7144864b0e4eec369bfde80068f9095dc63b37058bec48'

module web './container-app.bicep' = {
  name: 'web-${take(resourceToken, 6)}'
  params: {
    name: containerAppName
    location: location
    tags: tags
    environmentId: containerAppsEnvironment.id
    identityId: workloadIdentity.id
    registryServer: containerRegistry.properties.loginServer
    image: currentOrBootstrapImage
    externalIngress: externalIngress
  }
  dependsOn: [
    acrPull
  ]
}

output containerAppsEnvironmentName string = containerAppsEnvironment.name
output containerRegistryEndpoint string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name
output webName string = web.outputs.name
output webUri string = web.outputs.uri
