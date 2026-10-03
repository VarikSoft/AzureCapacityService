namespace AzureCapacityService.Models;

public class QuotaResponse
{
    public required string SubscriptionId { get; set; }
    public required string Region { get; set; }
    
    public required IReadOnlyList<QuotaInfo> Quotas { get; set; } // We use IReadOnlyList because any outer code should not change from the interface it
}