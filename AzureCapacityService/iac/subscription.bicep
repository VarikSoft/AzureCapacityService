
targetScope = 'subscription'

@description('Azure region for deployment')
param location string = 'swedencentral'

@description('Resource Group name')
param resourceGroupName string = 'rg-azure-capacity-dev'

@description('Globally unique Web App name')
param appName string

// Create Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

// Deploy application infrastructure inside Resource Group
module appInfrastructure './main.bicep' = {
  name: 'deploy-capacity-app'
  scope: resourceGroup

  params: {
    location: location
    appName: appName
  }
}

// Grant the Web App identity access to Azure resources
module quotaRoleAssignment './roleAssignment.bicep' = {
  name: 'assign-quota-reader'
  scope: subscription()

  params: {
    principalId: appInfrastructure.outputs.webAppPrincipalId
  }
}