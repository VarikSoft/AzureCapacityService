using AzureCapacityService.Models;

namespace AzureCapacityService.Services;

public class QuotaService : IQuotaService
{
    public QuotaInfo? GetQuota(string subscriptionId, string region, string quotaName) // Method can return QuotaInfo or Null
    {
        var quotas = GetQuotas(subscriptionId, region);

        return quotas.FirstOrDefault(quota => quota.Name.Equals(quotaName, StringComparison.OrdinalIgnoreCase)); 
        // That means we will find the first similar object or will return null
        // And in Equals we will ignore case of the strings
    }
    
    public List<QuotaInfo> GetQuotas(string subscriptionId, string region)
    {
        return new List<QuotaInfo>
        {
            new QuotaInfo
            {
                Name = "standardDSv5Family",
                DisplayName = "Standard DSv5 Family vCPUs",
                Limit = 20,
                Usage = 8,
                Unit = "Count"
            }
        };
    }
}