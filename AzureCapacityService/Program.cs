using AzureCapacityService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // This says to AspNet that I have controllers in the project, so please register them

builder.Services.AddScoped<IQuotaService, QuotaService>(); // ASP.NET now registers that this service can be creatable

var app = builder.Build();

app.MapControllers(); // This says, please use routes from the attributes [Route], [HttpGet] etc.

app.Run();