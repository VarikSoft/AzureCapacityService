targetScope = 'subscription'

@description('Azure region for the resource group')
param location string = 'swedencentral'

@description('Resource group name')
param resourceGroupName string = 'rg-azure-capacity-dev'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}
