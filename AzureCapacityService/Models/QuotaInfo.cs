namespace AzureCapacityService.Models;

public class QuotaInfo
{
    public required  string Name { get; set; }
    public required  string DisplayName { get; set; }
    
    public int Limit { get; set; }
    public int Usage { get; set; }
    public int Available => Limit - Usage; 
    
    public required string Unit { get; set; }
}