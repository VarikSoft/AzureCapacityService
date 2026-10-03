using Azure.ResourceManager;
using AzureCapacityService.Models;

namespace AzureCapacityService.Clients;

public interface IAzureQuotaClient
{
    Task<List<QuotaInfo>> GetQuotasAsync(string subscriptionId, string region, CancellationToken cancellationToken);
}