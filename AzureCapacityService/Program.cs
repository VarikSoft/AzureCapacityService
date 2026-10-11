using Azure.Identity;
using Azure.ResourceManager;
using Azure.Core.Pipeline;

using AzureCapacityService.Clients;
using AzureCapacityService.Services;
using AzureCapacityService.Exceptions;
using AzureCapacityService.Options;

var builder = WebApplication.CreateBuilder(args);

var credential = new AzureCliCredential(); // For local development (when we will go production -> ManagedIdentityCredential)
builder.Services.Configure<AzureOptions>(
    builder.Configuration.GetSection("Azure"));

builder.Services.AddHttpClient("AzureSdk"); // I've decided not to broke current AzureQuotaClient but also try to integrate HttpClientFactory (so I could easily replace it with needed one in future updates) 

builder.Services.AddOpenApi();

/* HttpClientFactory is used to create the HTTP client used by the Azure SDK transport
This keeps HTTP client lifetime management centralized while preserving the existing ArmClient-based Azure SDK implementation */
builder.Services.AddSingleton<ArmClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("AzureSdk");

    var options = new ArmClientOptions
    {
        Transport = new HttpClientTransport(httpClient)
    };

    return new ArmClient(credential, default, options);
});

builder.Services.AddControllers(); // This says to AspNet that I have controllers in the project, so please register them
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IQuotaService, QuotaService>(); // ASP.NET now registers that this service can be creatable
builder.Services.AddScoped<IAzureQuotaClient, AzureQuotaClient>();

var app = builder.Build();
app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/openapi/v1.json",
        "Azure Capacity Service v1");
});

app.UseExceptionHandler(); // This says our app to use GlobalExceptionHandler
app.MapControllers(); // This says, please use routes from the attributes [Route], [HttpGet] etc.

app.Run();

// NuGet packages: Azure.Identity, Azure.ResourceManager.Quota