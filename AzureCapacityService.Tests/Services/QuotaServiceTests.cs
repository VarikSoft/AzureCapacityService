using AzureCapacityService.Clients;
using AzureCapacityService.Models;
using AzureCapacityService.Services;

using Moq;

namespace AzureCapacityService.Tests.Services;

public class QuotaServiceTests
{
    [Fact]
    public async Task GetQuotaAsync_ShouldFindQuota_IgnoringCase()
    {
        // Arrange
        var quotaClientMock = new Mock<IAzureQuotaClient>();

        var quotas = new List<QuotaInfo>
        {
            new()
            {
                Name = "StandardDSv5Family",
                DisplayName = "Standard DSv5 Family vCPUs",
                Limit = 20,
                Usage = 8,
                Unit = "Count"
            }
        };

        quotaClientMock
            .Setup(client => client.GetQuotasAsync(
                "test-subscription",
                "westeurope",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        var service = new QuotaService(quotaClientMock.Object);

        // Act
        var result = await service.GetQuotaAsync(
            "test-subscription",
            "westeurope",
            "standarddsv5family",
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("StandardDSv5Family", result.Name);
    }
    
    [Fact]
    public async Task GetQuotaAsync_ShouldReturnNull_WhenQuotaDoesNotExist()
    {
        // Arrange
        var quotaClientMock = new Mock<IAzureQuotaClient>();

        var quotas = new List<QuotaInfo>
        {
            new()
            {
                Name = "StandardDSv5Family",
                DisplayName = "Standard DSv5 Family vCPUs",
                Limit = 20,
                Usage = 8,
                Unit = "Count"
            }
        };

        quotaClientMock
            .Setup(client => client.GetQuotasAsync(
                "test-subscription",
                "westeurope",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        var service = new QuotaService(quotaClientMock.Object);

        // Act
        var result = await service.GetQuotaAsync(
            "test-subscription",
            "westeurope",
            "NonExistingQuota",
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetQuotaAsync_ShouldReturnSpecificQuota_WhenQuotaExists()
    {
        // Arrange
        var quotaClientMock = new Mock<IAzureQuotaClient>();

        var quotas = new List<QuotaInfo>
        {
            new()
            {
                Name = "StandardDSv5Family",
                DisplayName = "Standard DSv5 Family vCPUs",
                Limit = 20,
                Usage = 8,
                Unit = "Count"
            },
            new()
            {
                Name = "StandardDv5Family",
                DisplayName = "Standard Dv5 Family vCPUs",
                Limit = 10,
                Usage = 2,
                Unit = "Count"
            }
        };

        quotaClientMock
            .Setup(client => client.GetQuotasAsync(
                "test-subscription",
                "westeurope",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        var service = new QuotaService(quotaClientMock.Object);

        // Act
        var result = await service.GetQuotaAsync(
            "test-subscription",
            "westeurope",
            "StandardDv5Family",
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("StandardDv5Family", result.Name);
        Assert.Equal(10, result.Limit);
        Assert.Equal(2, result.Usage);
        Assert.Equal(8, result.Available);
    }
    
    [Fact]
    public async Task GetQuotasAsync_ShouldReturnQuotasFromAzureClient()
    {
        // Arrange
        var quotaClientMock = new Mock<IAzureQuotaClient>();

        var quotas = new List<QuotaInfo>
        {
            new()
            {
                Name = "StandardDSv5Family",
                DisplayName = "Standard DSv5 Family vCPUs",
                Limit = 20,
                Usage = 8,
                Unit = "Count"
            },
            new()
            {
                Name = "StandardDv5Family",
                DisplayName = "Standard Dv5 Family vCPUs",
                Limit = 10,
                Usage = 2,
                Unit = "Count"
            }
        };

        quotaClientMock
            .Setup(client => client.GetQuotasAsync(
                "test-subscription",
                "westeurope",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(quotas);

        var service = new QuotaService(quotaClientMock.Object);

        // Act
        var result = await service.GetQuotasAsync(
            "test-subscription",
            "westeurope",
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("StandardDSv5Family", result[0].Name);
        Assert.Equal("StandardDv5Family", result[1].Name);
    }
}