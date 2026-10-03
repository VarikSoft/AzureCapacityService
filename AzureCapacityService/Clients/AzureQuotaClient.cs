using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Quota;
using Azure.ResourceManager.Quota.Models;

using Microsoft.Extensions.Options;

using AzureCapacityService.Options;
using AzureCapacityService.Exceptions;
using AzureCapacityService.Models;

namespace AzureCapacityService.Clients;

public class AzureQuotaClient : IAzureQuotaClient
{
    private readonly ArmClient _armClient;
    private readonly AzureOptions _azureOptions;

    public AzureQuotaClient(ArmClient armClient, IOptions<AzureOptions> options)
    {
        _armClient = armClient;
        _azureOptions = options.Value;
    }
    
    public async Task<List<QuotaInfo>> GetQuotasAsync(string subscriptionId, string region, CancellationToken cancellationToken)
    {
        /* To test cancellationToken
         try
        {
            await Task.Delay(10000, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Request was cancelled.");
            throw;
        }
        */
        try
        {
            var scope =
                $"/subscriptions/{subscriptionId}" +
                $"/providers/{_azureOptions.ResourceProvider}" +
                $"/locations/{region}";

            var scopeId = new ResourceIdentifier(scope);
            var quotaCollection = _armClient.GetCurrentQuotaLimitBases(scopeId);
            var usageCollection = _armClient.GetCurrentUsagesBases(scopeId);

            var quotas = new List<QuotaInfo>();
            var usages = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            await foreach (var usageResource in usageCollection.GetAllAsync(cancellationToken: cancellationToken))
            {
                var properties = usageResource.Data.Properties;

                usages[properties.Name.Value] = properties.Usages.Value;
            }

            await foreach (var quotaResource in quotaCollection.GetAllAsync(cancellationToken: cancellationToken))
            {
                var properties = quotaResource.Data.Properties;

                // Azure SDK exposes Limit as the base type QuotaLimitJsonObject
                // We check that the actual object is QuotaLimitObject so we can access its Value
                if (properties.Limit is not QuotaLimitObject limit)
                {
                    continue;
                }

                usages.TryGetValue(
                    properties.Name.Value,
                    out var usage);

                var quotaInfo = new QuotaInfo
                {
                    Name = properties.Name.Value,
                    DisplayName = properties.Name.LocalizedValue,
                    Limit = limit.Value,
                    Usage = usage,
                    Unit = properties.Unit
                };

                quotas.Add(quotaInfo);
            }

            return quotas;
        }
        catch (RequestFailedException ex)
        {
            throw new AzureServiceException(
                $"Azure Quota API request failed: {ex.Message}",
                ex.Status,
                ex);
        }
    }
}