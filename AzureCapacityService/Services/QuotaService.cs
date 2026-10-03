using AzureCapacityService.Clients;
using AzureCapacityService.Models;

namespace AzureCapacityService.Services;

public class QuotaService : IQuotaService
{
    private readonly IAzureQuotaClient _quotaClient;

    public QuotaService(IAzureQuotaClient quotaClient)
    {
        _quotaClient = quotaClient;
    }
    
    public async Task<QuotaInfo?> GetQuotaAsync(string subscriptionId, string region, string quotaName, CancellationToken cancellationToken) // Method can return QuotaInfo or Null
    {
        var quotas = await GetQuotasAsync(subscriptionId, region, cancellationToken);

        return quotas.FirstOrDefault(quota => quota.Name.Equals(quotaName, StringComparison.OrdinalIgnoreCase)); 
        // That means we will find the first similar object or will return null
        // And in Equals we will ignore case of the strings
    }
    
    public async Task<List<QuotaInfo>> GetQuotasAsync(string subscriptionId, string region, CancellationToken cancellationToken)
    {
        return await _quotaClient.GetQuotasAsync(subscriptionId, region, cancellationToken);
    }
}