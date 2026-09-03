@description('Container App resource name.')
param name string

@description('Azure region.')
param location string

@description('Tags applied to the Container App.')
param tags object

@description('Container Apps environment resource ID.')
param environmentId string

@description('User-assigned identity resource ID.')
param identityId string

@description('Azure Container Registry login server.')
param registryServer string

@description('Immutable current image or first-deployment bootstrap image.')
param image string

@description('Whether ingress is externally accessible.')
param externalIngress bool

resource web 'Microsoft.App/containerApps@2026-01-01' = {
  name: name
  location: location
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    environmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: false
        external: externalIngress
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          identity: identityId
          server: registryServer
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: image
          env: [
            {
              name: 'ASPNETCORE_HTTP_PORTS'
              value: '8080'
            }
            {
              name: 'DOTNET_ENVIRONMENT'
              value: 'Production'
            }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 2
              periodSeconds: 3
              timeoutSeconds: 2
              failureThreshold: 10
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 5
              periodSeconds: 20
              timeoutSeconds: 2
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 2
              periodSeconds: 5
              timeoutSeconds: 2
              failureThreshold: 6
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '25'
              }
            }
          }
        ]
      }
    }
  }
}

output name string = web.name
output uri string = 'https://${web.properties.configuration.ingress.fqdn}'
