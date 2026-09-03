targetScope = 'subscription'

@minLength(2)
@maxLength(22)
@description('Lowercase alphanumeric/hyphen environment name used to generate resource names.')
param environmentName string

@description('Azure region for all resources.')
param location string

@description('Set by Azure Developer CLI when the web Container App already exists.')
param webExists bool

@description('Expose the web ingress outside the Container Apps environment.')
param externalIngress bool = true

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
  application: 'carddemo-azure'
  workload: 'mainframe-modernization'
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module workload './resources.bicep' = {
  name: 'carddemo-${resourceToken}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    resourceToken: resourceToken
    tags: tags
    webExists: webExists
    externalIngress: externalIngress
  }
}

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = workload.outputs.containerAppsEnvironmentName
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = workload.outputs.containerRegistryEndpoint
output AZURE_CONTAINER_REGISTRY_NAME string = workload.outputs.containerRegistryName
output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = resourceGroup.name
output SERVICE_WEB_NAME string = workload.outputs.webName
output SERVICE_WEB_URI string = workload.outputs.webUri
