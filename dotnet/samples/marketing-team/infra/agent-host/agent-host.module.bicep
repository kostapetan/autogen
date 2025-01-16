@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param outputs_azure_container_registry_managed_identity_id string

param outputs_managed_identity_client_id string

param outputs_azure_container_apps_environment_id string

resource agent_host 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'agent-host'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5001
        transport: 'http2'
      }
    }
    environmentId: outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'kpetan.azurecr.io/autogen/agent-host:v1'
          name: 'agent-host'
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'https://+;http://+'
            }
            {
              name: 'ASPNETCORE_HTTPS_PORTS'
              value: '5001'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: outputs_managed_identity_client_id
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}