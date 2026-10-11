# Infrastructure as Code (IaC)

## Overview

The infrastructure for the Azure Capacity Service is described using **Bicep**.

The current IaC solution deploys the application infrastructure required for the MVP and configures Azure access for the Web App.

The structure is:

```text
iac/
├── main.bicep
└── roleAssignment.bicep
```

## Why Bicep Was Chosen

Bicep was selected instead of Terraform because this project is Azure-specific.

The service already uses Azure Resource Manager and Azure SDK, so Bicep fits naturally into the existing Azure stack.

Main reasons for choosing Bicep:

- native Azure Resource Manager integration;
- no separate Terraform provider configuration;
- no separate Terraform state file to manage;
- direct support for Azure resource types and properties;
- simple syntax for a relatively small Azure-only MVP;
- easy integration with Azure CLI.

Terraform would be a stronger choice if the project needed multi-cloud support or if the infrastructure was already managed through Terraform.

## Resources Deployed

The current template creates:

<p align="left">
  <img src="https://i.imgur.com/3iXG7t3.png" width="300">
</p>


### App Service Plan

The App Service Plan provides the compute environment for the Web App.

Its SKU is configurable through:

```bicep
param skuName string = 'F1'
```

### Azure Web App

The Web App hosts the ASP.NET Core API.

It is connected to the App Service Plan through:

```bicep
serverFarmId: appServicePlan.id
```

HTTPS-only access is enabled.

### Application Configuration

The Azure resource provider is passed to the application as an environment variable:

```text
Azure__ResourceProvider
```

ASP.NET Core maps this to:

```text
Azure:ResourceProvider
```

This allows the existing `AzureOptions` configuration to work without code changes.

The default value is:

```text
Microsoft.Compute
```

## Managed Identity

The Web App uses a system-assigned managed identity:

```bicep
identity: {
  type: 'SystemAssigned'
}
```

This allows the deployed application to authenticate to Azure without storing credentials or secrets.

For local development, the application uses `AzureCliCredential`.

For Azure deployment, the managed identity can be used instead.

## RBAC

The Web App managed identity receives read access at subscription scope.

The MVP uses the built-in Azure `Reader` role because the application only reads quota and usage information.

The role assignment is deployed in a separate module:

```text
roleAssignment.bicep
```

This is required because `main.bicep` is deployed at resource-group scope, while the RBAC assignment is created at subscription scope.

The structure is:

```text
main.bicep
(resource group scope)

module roleAssignment.bicep
(subscription scope)
```

## Cross-Scope Module

`main.bicep` deploys the application resources at resource-group scope.

The subscription-level role assignment is delegated to the module:

```bicep
module quotaRoleAssignment 'roleAssignment.bicep' = {
  name: 'quotaReaderRoleAssignment'
  scope: subscription()

  params: {
    principalId: webApp.identity.principalId
  }
}
```

This keeps the deployment valid while allowing resources at different Azure scopes.

## Validation

The Bicep template can be compiled locally with Azure CLI:

```bash
az bicep build --file iac/main.bicep
```

A successful build generates the corresponding ARM JSON template and validates the Bicep syntax.

## Deployment Example

The template can be deployed to an existing resource group with Azure CLI:

```bash
az deployment group create \
  --resource-group <resource-group-name> \
  --template-file iac/main.bicep \
  --parameters appName=<unique-app-name>
```

The account performing the deployment must have sufficient permissions to create the application resources and assign the subscription-level RBAC role.

## Future Improvements

Possible IaC improvements include:

- separate parameter files for development and production;
- a more restrictive custom RBAC role instead of the general Reader role;
- deployment slots;
- Application Insights;
- CI/CD deployment of Bicep templates;
- environment-specific App Service SKUs;
- Key Vault integration if secrets are introduced later.