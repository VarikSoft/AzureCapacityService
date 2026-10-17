namespace AzureCapacityService.Services;

public interface IKeyVaultService
{
    Task<bool> VerifySecretAccessAsync(
        CancellationToken cancellationToken);
}