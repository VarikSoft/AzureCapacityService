namespace AzureCapacityService.Models;

public class QuotaInfo
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    
    public int Limit { get; set; }
    public int Usage { get; set; }
    public int Available => Limit - Usage; 
    
    public string Unit { get; set; }
}