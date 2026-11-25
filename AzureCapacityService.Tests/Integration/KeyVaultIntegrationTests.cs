using Azure;
using Azure.Security.KeyVault.Secrets;
using AzureCapacityService.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AzureCapacityService.Tests.Integration;

public class KeyVaultIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task VerifySecretAccess_UsesRegisteredServiceAndAzureSdkClient()
    {
        // Keep CI independent of Azure credentials and live secrets.
        // The real KeyVaultService and application DI are used; only the external SDK call is mocked.
        var secretClient = new Mock<SecretClient>();
        var secret = new KeyVaultSecret("DeploymentVerificationSecret", "test-value");

        secretClient
            .Setup(client => client.GetSecretAsync(
                "DeploymentVerificationSecret",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(secret, Mock.Of<Response>()));

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<SecretClient>();
                services.AddSingleton(secretClient.Object);
            }));

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

        var canReadSecret = await service.VerifySecretAccessAsync(CancellationToken.None);

        Assert.True(canReadSecret);
        secretClient.Verify(client => client.GetSecretAsync(
            "DeploymentVerificationSecret",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
