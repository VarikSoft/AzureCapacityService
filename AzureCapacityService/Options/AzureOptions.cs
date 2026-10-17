namespace AzureCapacityService.Options;

public class AzureOptions
{
    public required string ResourceProvider { get; init; }
    
    public string? SubscriptionId { get; init; }

    public string? DefaultRegion { get; init; }
}