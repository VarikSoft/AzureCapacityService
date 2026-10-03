using Azure.Identity;
using Azure.ResourceManager;

using AzureCapacityService.Clients;
using AzureCapacityService.Services;
using AzureCapacityService.Exceptions;
using AzureCapacityService.Options;

var builder = WebApplication.CreateBuilder(args);

var credential = new AzureCliCredential(); // For local development (when we will go production -> ManagedIdentityCredential)
builder.Services.Configure<AzureOptions>(
    builder.Configuration.GetSection("Azure"));

builder.Services.AddOpenApi();

builder.Services.AddSingleton(new ArmClient(credential));

builder.Services.AddControllers(); // This says to AspNet that I have controllers in the project, so please register them
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IQuotaService, QuotaService>(); // ASP.NET now registers that this service can be creatable
builder.Services.AddScoped<IAzureQuotaClient, AzureQuotaClient>();

var app = builder.Build();
app.MapOpenApi();

app.UseExceptionHandler(); // This says our app to use GlobalExceptionHandler
app.MapControllers(); // This says, please use routes from the attributes [Route], [HttpGet] etc.

app.Run();

// NuGet packages: Azure.Identity, Azure.ResourceManager.Quota