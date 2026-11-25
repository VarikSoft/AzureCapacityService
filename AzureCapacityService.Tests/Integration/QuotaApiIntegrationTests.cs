using System.Net;
using System.Net.Http.Json;
using AzureCapacityService.Models;
using AzureCapacityService.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AzureCapacityService.Tests.Integration;

public class QuotaApiIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetQuotas_ReturnsExpectedJsonThroughHttpPipeline()
    {
        var quotaService = new Mock<IQuotaService>();
        quotaService
            .Setup(service => service.GetQuotasAsync(
                "test-subscription",
                "westeurope",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QuotaInfo>
            {
                new()
                {
                    Name = "StandardDSv5Family",
                    DisplayName = "Standard DSv5 Family vCPUs",
                    Limit = 20,
                    Usage = 8,
                    Unit = "Count"
                }
            });

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IQuotaService>();
                services.AddScoped(_ => quotaService.Object);
            }));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            "/api/subscriptions/test-subscription/regions/westeurope/quotas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<QuotaResponse>();
        Assert.NotNull(body);
        Assert.Equal("test-subscription", body.SubscriptionId);
        Assert.Equal("westeurope", body.Region);
        var quota = Assert.Single(body.Quotas);
        Assert.Equal("StandardDSv5Family", quota.Name);
        Assert.Equal(12, quota.Available);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMissingQuota_Returns404ThroughHttpPipeline()
    {
        var quotaService = new Mock<IQuotaService>();
        quotaService
            .Setup(service => service.GetQuotaAsync(
                "test-subscription",
                "westeurope",
                "missing",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotaInfo?)null);

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IQuotaService>();
                services.AddScoped(_ => quotaService.Object);
            }));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            "/api/subscriptions/test-subscription/regions/westeurope/quotas/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
