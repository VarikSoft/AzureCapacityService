namespace AzureCapacityService.Models;

public class QuotaResponse
{
    public string SubscriptionId { get; set; }
    public string Region { get; set; }
    
    public IReadOnlyList<QuotaInfo> Quotas { get; set; } // We use IReadOnlyList because any outer code should not change from the interface it
}