targetScope = 'resourceGroup'

@description('Key Vault name')
param keyVaultName string

@description('Web App Managed Identity principal ID')
param principalId string

var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, secretsUserRoleId)
  scope: keyVault

  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      secretsUserRoleId
    )
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
