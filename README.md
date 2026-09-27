# Azure Subscription Quota Service

## 1. Overview

The purpose of the application is to retrieve Azure subscription quota and capacity information.

The service allows a client to retrieve quota information for a selected Azure subscription and region.

The initial MVP is read-only and focuses on retrieving:

- quota limits;
- current usage;
- available capacity;
- all quotas for the selected region;
- a specific quota when required.

Quota modification is not included in the initial MVP and can be added as a future extension.

## 2. MVP Scope

The application will support the following flow:

![Diagram 1](https://i.imgur.com/G31MR9J.png)

The application does not include a graphical user interface.

The API can initially be tested using Swagger or Postman.

## 3. API Contract

### Get all quotas for a region

```
GET /api/subscriptions/{subscriptionId}/regions/{region}/quotas
```

Example:

```http
GET /api/subscriptions/12345678/regions/westeurope/quotas
```

The endpoint returns all available quota information for the selected subscription and region.

### Get a specific quota

```http
GET /api/subscriptions/{subscriptionId}/regions/{region}/quotas/{quotaName}
```

Example:

```http
GET /api/subscriptions/12345678/regions/westeurope/quotas/standardDSv5Family
```

This endpoint returns quota information for one specific resource.

## 4. API Response

Example response:

```json
{
  "subscriptionId": "12345678",
  "region": "westeurope",
  "quotas": [
    {
      "name": "standardDSv5Family",
      "displayName": "Standard DSv5 Family vCPUs",
      "limit": 20,
      "usage": 8,
      "available": 12,
      "unit": "Count"
    },
    {
      "name": "standardDv5Family",
      "displayName": "Standard Dv5 Family vCPUs",
      "limit": 10,
      "usage": 2,
      "available": 8,
      "unit": "Count"
    }
  ]
}
```

Available capacity is calculated as:

```text
Available = Limit - Usage
```

Azure provides quota limits and current usage separately for a specified scope.

## 5. Architecture

The application will use a simple layered architecture.

![Diagram 2](https://i.imgur.com/of2KKA1.png)

### QuotaController

Responsible for:

- receiving HTTP requests;
- validating required parameters;
- returning HTTP responses;
- mapping exceptions to appropriate HTTP status codes.

### QuotaService

Responsible for application logic:

- retrieving quota limits;
- retrieving current usage;
- combining quota and usage information;
- calculating available capacity;
- finding a specific quota.

### AzureQuotaClient

Responsible for communication with Azure.

It isolates Azure SDK-specific implementation from the rest of the application.

## 6. Azure Integration

The MVP will initially focus on:

```text
Microsoft.Compute
```

Quota information is retrieved using an Azure Resource Manager scope.

Example:

```text
/subscriptions/{subscriptionId}
/providers/Microsoft.Compute
/locations/{region}
```

Azure Quota API can then retrieve quota limits and current usage for that scope.

Conceptually:

![Diagram 3](https://i.imgur.com/Ji60OJY.png)

This is why `region` is required when retrieving Compute quotas.

## 7. Authentication

The application will authenticate to Azure using Azure Identity.

For development, the application can use:

```csharp
DefaultAzureCredential
```

Conceptually:

![Diagram 4](https://i.imgur.com/J0F0L5o.png)

`DefaultAzureCredential` can use supported developer credentials such as Azure CLI or Visual Studio credentials when running locally.

Credentials and access tokens must not be stored directly in the source code.

For deployment to Azure, authentication can later be changed to Managed Identity.

## 8. Data Model

The main application model can look like:

```csharp
public sealed class QuotaInfo
{
    public string Name { get; set; }
    public string DisplayName { get; set; }

    public int Limit { get; set; }
    public int Usage { get; set; }

    public int Available => Limit - Usage;

    public string Unit { get; set; }
}
```

Response model:

```csharp
public sealed class QuotaResponse
{
    public string SubscriptionId { get; set; }
    public string Region { get; set; }

    public IReadOnlyCollection<QuotaInfo> Quotas { get; set; }
}
```

The application does not require a database for the initial MVP because quota information is retrieved directly from Azure.

## 9. Error Handling

The application should handle common error scenarios.

### Invalid request

Examples:

- missing subscription ID;
- missing region;
- invalid quota name.

Response:

```text
400 Bad Request
```

### Authentication failure

```text
401 Unauthorized
```

### Insufficient Azure permissions

```text
403 Forbidden
```

### Subscription or quota not found

```text
404 Not Found
```

### Azure throttling

```text
429 Too Many Requests
```

### Azure API failure

```text
502 Bad Gateway
```

The original Azure error and request information should be logged for debugging.

## 10. Configuration

Application configuration can be stored in:

```text
appsettings.json
```

Example:

```json
{
  "Azure": {
    "ResourceProvider": "Microsoft.Compute"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Values such as:

```text
Subscription ID
Region
Quota Name
```

should not be hardcoded into the configuration because they are parameters of individual API requests.

Secrets or Azure credentials must not be stored in `appsettings.json`.

## 11. Request Flow

Example request:

```http
GET /api/subscriptions/12345678/regions/westeurope/quotas
```

Processing flow:

![Diagram 5](https://i.imgur.com/H9Vsozp.png)

## 12. MVP Project Structure

A possible project structure:

```text
AzureQuotaService/
│
├── Controllers/
│   └── QuotaController.cs
│
├── Services/
│   └── QuotaService.cs
│
├── Clients/
│   └── AzureQuotaClient.cs
│
├── Models/
│   ├── QuotaInfo.cs
│   └── QuotaResponse.cs
│
├── Configuration/
│   └── AzureOptions.cs
│
├── Program.cs
├── appsettings.json
│
├── README.md
└── DESIGN.md
```

The MVP remains a single ASP.NET Core application.

Additional projects or microservices are not required at this stage.

## 13. Testing

The initial version should include unit tests for:

- available capacity calculation;
- quota and usage mapping;
- quota search by name;
- invalid input;
- Azure client error handling.

Integration tests can later verify communication with a real Azure subscription.

## 14. Future Extensions

After the MVP is completed, the application can be extended with additional functionality.

Possible extensions:

### Quota modification

Support quota increase requests through Azure Quota API.

Azure provides a PUT operation for creating or updating quota limits.

### CRUD / Azure SDK operations

Explore additional Create, Read, Update and Delete operations available through Azure SDK for supported Azure resources.

This can be implemented as a separate extension after the read-only quota MVP is completed.

### Additional Resource Providers

Support providers other than:

```text
Microsoft.Compute
```

For example:

```text
Microsoft.Network
Microsoft.MachineLearningServices
```

### User Interface

Add a graphical interface for selecting:

```text
Subscription
Region
Quota
```

and displaying quota utilization.

### Monitoring

Add alerts when quota usage approaches its limit.

Example:

```text
Quota usage > 80%
        ↓
Warning / Alert
```

### Caching

Cache quota information for a short period to reduce unnecessary Azure API requests.

## 15. MVP Summary

The first version of the application is intentionally simple.

![Diagram 6](https://i.imgur.com/nj2ZdpH.png)

The MVP is read-only.

Quota modification, additional Azure SDK CRUD operations, UI, monitoring and support for additional Azure resource providers are planned as future extensions.