using AzureCapacityService.Models;

namespace AzureCapacityService.Services;

public interface IQuotaService
{
    Task<List<QuotaInfo>> GetQuotasAsync(string subscriptionId, string region, CancellationToken cancellationToken);
    
    Task<QuotaInfo?> GetQuotaAsync(string subscriptionId, string region, string quotaId, CancellationToken cancellationToken);
}