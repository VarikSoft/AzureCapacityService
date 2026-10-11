param location string = resourceGroup().location

param appName string

param resourceProvider string = 'Microsoft.Compute'

param skuName string = 'F1'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location

  sku: {
    name: skuName
  }

  kind: 'linux'

  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'

  identity: {
    type: 'SystemAssigned'
  }

  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true

    siteConfig: {
      appSettings: [
        {
          name: 'Azure__ResourceProvider'
          value: resourceProvider
        }
      ]
    }
  }
}