
targetScope = 'resourceGroup'

@description('Azure region')
param location string = resourceGroup().location

@description('Globally unique Web App name')
param appName string

@description('Azure resource provider for quota queries')
param resourceProvider string = 'Microsoft.Compute'

@description('App Service Plan SKU')
param skuName string = 'F1'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'

  sku: {
    name: skuName
  }

  properties: {
    reserved: true
  }
}

// Azure Web App
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
      linuxFxVersion: 'DOTNETCORE|10.0'

      appSettings: [
        {
          name: 'Azure__ResourceProvider'
          value: resourceProvider
        }
        {
          name: 'KeyVault__VaultUri'
          value: keyVault.properties.vaultUri
        }
      ]
    }
  }
}

// Azure Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${uniqueString(resourceGroup().id)}'
  location: location

  properties: {
    tenantId: subscription().tenantId

    sku: {
      family: 'A'
      name: 'standard'
    }

    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
  }
}

// Built-in Key Vault Secrets User role
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    keyVault.id,
    webApp.name,
    keyVaultSecretsUserRoleId
  )

  scope: keyVault

  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleId
    )

    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs for other Bicep modules
output webAppPrincipalId string = webApp.identity.principalId
output webAppName string = webApp.name
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri