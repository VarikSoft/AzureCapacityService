## 1. Overview

The purpose of the application is to retrieve Azure subscription quota and capacity information.

The service allows a client to retrieve quota information for a selected Azure subscription and region.

The MVP is read-only and supports retrieving:

- quota limits;
- current usage;
- available capacity;
- all quotas for a selected region;
- a specific quota by name.

Quota modification is not included in the MVP and can be added as a future extension.



## 2. MVP Scope

The application follows this general flow:

<p align="left">
  <img src="https://i.imgur.com/78RJ7WG.png" width="200">
</p>

The application does not include a graphical user interface.

The API can be tested directly through HTTP clients such as Postman, curl, or a browser for GET requests.

The service also publishes an OpenAPI document that describes the available endpoints, parameters, response models, and HTTP status codes.

Local OpenAPI document:

```text
/openapi/v1.json
```



## 3. API Contract

### Get all quotas for a region

```http
GET /api/subscriptions/{subscriptionId}/regions/{region}/quotas
```

Example:

```http
GET /api/subscriptions/12345678/regions/westeurope/quotas
```

The endpoint returns all available quota information for the selected subscription and region.

Possible responses:

```text
200 OK
400 Bad Request
403 Forbidden
429 Too Many Requests
502 Bad Gateway
```

### Get a specific quota

```http
GET /api/subscriptions/{subscriptionId}/regions/{region}/quotas/{quotaName}
```

Example:

```http
GET /api/subscriptions/12345678/regions/westeurope/quotas/standardDSv5Family
```

The endpoint returns quota information for one specific quota.

Possible responses:

```text
200 OK
400 Bad Request
403 Forbidden
404 Not Found
429 Too Many Requests
502 Bad Gateway
```



## 4. API Response

Example response for all quotas:

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

Azure provides quota limits and current usage separately for the specified resource scope.



## 5. Architecture

The application uses a simple layered architecture.

<p align="left">
  <img src="https://i.imgur.com/Ufrngjn.png" width="200">
</p>

### QuotaController

Responsible for:

- receiving HTTP requests;
- validating required request parameters;
- calling the application service;
- returning HTTP responses;
- exposing response metadata for OpenAPI.

The controller does not contain Azure SDK-specific logic.

### QuotaService

Responsible for application-level logic:

- requesting quota information through `IAzureQuotaClient`;
- finding a specific quota by name;
- keeping application logic separated from Azure SDK implementation.

The specific quota search is case-insensitive.

### AzureQuotaClient

Responsible for Azure-specific communication:

- creating the Azure Resource Manager scope;
- retrieving quota limits;
- retrieving current usage;
- matching usage values with quota limits;
- mapping Azure SDK models to application models;
- converting Azure SDK failures into application-specific exceptions.

### GlobalExceptionHandler

Responsible for centralized exception handling.

It:

- handles `AzureServiceException`;
- maps Azure-related failures to HTTP response codes;
- logs internal Azure exception information;
- returns a safe `ErrorResponse` to the API client.

### Dependency Injection

Dependencies are registered through ASP.NET Core dependency injection.

The application depends on abstractions such as:

```text
IQuotaService
IAzureQuotaClient
```

instead of depending directly on concrete implementations.

This makes the service easier to extend and allows dependencies to be replaced or mocked during testing.

### Async and Cancellation

The HTTP request flow is asynchronous end-to-end:

<p align="center">
  <img src="https://i.imgur.com/BGuiNcD.png" height="200">
</p>

`CancellationToken` is propagated through the complete request chain and passed to Azure SDK asynchronous operations.

If the original HTTP request is cancelled, the cancellation can propagate to the Azure operation instead of continuing unnecessary work.



## 6. API Style: Controllers vs Minimal APIs

ASP.NET Core Controllers were selected instead of Minimal APIs.

Both approaches are suitable for a small API. Minimal APIs could provide a simpler implementation for the current MVP, but Controllers were selected because the service uses a layered architecture and is expected to grow.

Controllers provide:

- clear separation between HTTP handling and application logic;
- attribute-based routing;
- explicit response metadata;
- straightforward OpenAPI integration;
- dependency injection through constructors;
- a structure that can later be extended with authorization, filters, and validation.

For this project, Controllers provide a clearer structure while keeping HTTP concerns separated from Azure-specific and application logic.



## 7. Azure Integration

The MVP currently uses:

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

In code, the resource provider is read from configuration rather than hardcoded directly into the client.

The Azure SDK is used to retrieve:

- current quota limits;
- current quota usage.

The returned quota and usage collections are matched by quota name.

Conceptually:

<p align="left">
  <img src="https://i.imgur.com/Ezw0NRN.png" width="250">
</p>

This is why `region` is required when retrieving Microsoft Compute quotas.

## 8. Authentication

The application uses Azure Identity for authentication.

For local development, the application currently uses:

```csharp
AzureCliCredential
```

The developer authenticates through Azure CLI:

```text
az login
```

The application then uses the authenticated Azure CLI identity when creating the Azure Resource Manager client.

Credentials and access tokens are not stored directly in source code or `appsettings.json`.

For deployment to Azure, authentication can later use:

```csharp
ManagedIdentityCredential
```

This avoids storing credentials in the deployed application.

`DefaultAzureCredential` can also be considered in environments where a credential chain is preferred.

Conceptually:

<p align="left">
  <img src="https://i.imgur.com/RE2Sh3k.png" width="200">
</p>

## 9. Data Models

### QuotaInfo

```csharp
public class QuotaInfo
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }

    public int Limit { get; init; }
    public int Usage { get; init; }

    public int Available => Limit - Usage;

    public required string Unit { get; init; }
}
```

### QuotaResponse

```csharp
public class QuotaResponse
{
    public required string SubscriptionId { get; init; }
    public required string Region { get; init; }
    public required IReadOnlyList<QuotaInfo> Quotas { get; init; }
}
```

### ErrorResponse

```csharp
public class ErrorResponse
{
    public required string Error { get; init; }
    public required string Message { get; init; }
}
```

`required` ensures that mandatory non-nullable properties are initialized when an object is created.

`init` is used because these response models are created once and are not expected to be modified afterward.

The application does not require a database for the MVP because quota information is retrieved directly from Azure.

## 10. Error Handling

The application uses centralized exception handling.

Azure SDK operations can throw:

```text
RequestFailedException
```

`AzureQuotaClient` catches Azure SDK failures and converts them into:

```text
AzureServiceException
```

`GlobalExceptionHandler` then maps the Azure status code to the API response.

Current mapping:

```text
400 -> 400 Bad Request
401 -> 401 Unauthorized
403 -> 403 Forbidden
404 -> 404 Not Found
429 -> 429 Too Many Requests
other Azure failures -> 502 Bad Gateway
```

Validation errors and a missing specific quota are handled directly by the controller.

The API returns a safe `ErrorResponse` to the client.

Example:

```json
{
  "error": "AzureServiceError",
  "message": "Azure service request failed."
}
```

Detailed exception information is kept in application logs instead of being returned to the client.

The application logs:

- the original exception;
- request path;
- Azure status code;
- final API response status code.

Logging is performed through:

```text
ILogger<GlobalExceptionHandler>
```

## 11. Configuration

Application configuration is stored in:

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

Azure configuration is represented by an options class:

```csharp
public class AzureOptions
{
    public required string ResourceProvider { get; init; }
}
```

The options class is registered through ASP.NET Core configuration:

```csharp
builder.Services.Configure<AzureOptions>(
    builder.Configuration.GetSection("Azure"));
```

`AzureQuotaClient` receives the configuration through:

```csharp
IOptions<AzureOptions>
```

Values such as:

```text
Subscription ID
Region
Quota Name
```

are not stored in application configuration because they are parameters of individual API requests.

Secrets and Azure credentials must not be stored in `appsettings.json`.

## 12. OpenAPI

The application publishes an OpenAPI specification using ASP.NET Core OpenAPI support.

OpenAPI is registered through:

```csharp
builder.Services.AddOpenApi();
```

and exposed through:

```csharp
app.MapOpenApi();
```

During local development, the specification is available at:

```text
http://localhost:5000/openapi/v1.json
```

The document describes:

- API routes;
- path parameters;
- response status codes;
- `QuotaInfo`;
- `QuotaResponse`;
- `ErrorResponse`.

Controller response metadata is declared through `ProducesResponseType` attributes.

The API produces JSON responses.

## 13. Request Flow

Example request:

```http
GET /api/subscriptions/12345678/regions/westeurope/quotas
```

Processing flow:

<p align="center">
  <img src="https://i.imgur.com/FYZ22QF.png" width="800">
</p>

## 14. MVP Project Structure

Current project structure:

```text
AzureCapacityService/
|
├── Clients/
|   ├── IAzureQuotaClient.cs
|   └── AzureQuotaClient.cs
|
├── Controllers/
|   └── QuotaController.cs
|
├── Exceptions/
|   ├── AzureServiceException.cs
|   └── GlobalExceptionHandler.cs
|
├── Models/
|   ├── ErrorResponse.cs
|   ├── QuotaInfo.cs
|   └── QuotaResponse.cs
|
├── Options/
|   └── AzureOptions.cs
|
├── Services/
|   ├── IQuotaService.cs
|   └── QuotaService.cs
|
├── Program.cs
├── appsettings.json
├── README.md
└── DESIGN.md
```

The MVP remains a single ASP.NET Core application.

Additional services or microservices are not required for the current scope.

## 15. Testing

The following unit tests are planned for the MVP:

- available capacity calculation;
- quota and usage mapping;
- quota search by name;
- invalid input handling;
- Azure client error handling.

Integration tests can later verify communication with a real Azure subscription.

Manual verification already includes:

- retrieving real quota limits from Azure;
- retrieving current quota usage;
- verifying end-to-end asynchronous calls;
- verifying `CancellationToken` propagation by cancelling an HTTP request;
- verifying exception mapping from Azure-related failures to API HTTP responses;
- verifying the generated OpenAPI document.

## 16. Future Extensions

After the MVP is completed, the application can be extended with additional functionality.

### Quota Modification

Support quota increase requests through Azure Quota API.

Azure provides operations for requesting or updating supported quota limits.

### CRUD / Azure SDK Operations

Explore additional Create, Read, Update, and Delete operations available through Azure SDK for supported Azure resources.

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

<p align="center">
  <img src="https://i.imgur.com/CPMAGJa.png" height="100">
</p>

### Caching

Cache quota information for a short period to reduce unnecessary Azure API requests.

### Background Processing

A background worker could periodically refresh quota information and calculate additional values such as:

```text
Usage percentage
Near-limit status
Last updated timestamp
```

## 17. MVP Summary

The first version of the application is intentionally simple and read-only.

<p align="center">
  <img src="https://i.imgur.com/JXmYRB4.png" width="800">
</p>

The implemented MVP includes:

- ASP.NET Core Controllers;
- dependency injection;
- asynchronous request processing;
- `CancellationToken` propagation;
- Azure SDK integration;
- quota limit retrieval;
- current usage retrieval;
- available capacity calculation;
- centralized error handling;
- structured logging;
- `appsettings.json`;
- Options Pattern;
- OpenAPI publication.

Quota modification, additional Azure SDK operations, UI, monitoring, caching, and support for additional Azure resource providers remain possible future extensions.
