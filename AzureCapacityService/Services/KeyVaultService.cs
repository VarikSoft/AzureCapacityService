using Azure.Security.KeyVault.Secrets;

namespace AzureCapacityService.Services;

public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;

    public KeyVaultService(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task<bool> VerifySecretAccessAsync(
        CancellationToken cancellationToken)
    {
        var response = await _secretClient.GetSecretAsync(
            "DeploymentVerificationSecret",
            cancellationToken: cancellationToken);

        return response.Value.Value is not null;
    }
}