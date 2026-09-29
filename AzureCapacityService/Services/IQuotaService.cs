using AzureCapacityService.Models;

namespace AzureCapacityService.Services;

public interface IQuotaService
{
    List<QuotaInfo> GetQuotas(string subscriptionId, string region);
    
    QuotaInfo? GetQuota(string subscriptionId, string region, string quotaId);
}