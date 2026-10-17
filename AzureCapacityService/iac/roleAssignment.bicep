
targetScope = 'subscription'

@description('Managed Identity principal ID')
param principalId string

// Built-in Azure Reader role
var readerRoleDefinitionId = 'acdd72a7-3385-48ef-bd42-f606fba81ae7'

// Assign Reader role at subscription scope
resource quotaReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    subscription().id,
    principalId,
    readerRoleDefinitionId
  )

  properties: {
    principalId: principalId

    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      readerRoleDefinitionId
    )

    principalType: 'ServicePrincipal'
  }
}