using AzureCapacityService.Models;
using AzureCapacityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureCapacityService.Controllers;

[ApiController] // Converts our class into API controller
[Route("api/subscriptions")] // defines a basic URL api/subscriptions
public class QuotaController : ControllerBase
{
    private readonly IQuotaService _quotaService; // Controller now keeps link to the service

    public QuotaController(QuotaService quotaService) // Constructor injection
    {
        _quotaService = quotaService;
    }
    
    // First Endpoint to Get all quotas list
    [HttpGet("{subscriptionId}/regions/{region}/quotas")] // Means that method procecces GET api/subscriptions/{subscriptionId}/regions/{region}/quotas
    public IActionResult GetQuotas(string subscriptionId, string region)
    {
        // Why IsNullOrWhiteSpace insted of IsNullOrEmpty, is that because IsNullOrWhiteSpace can catch strings like "    "
        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(region)) // I've added a primitive data validation
        {
            return BadRequest("Subscription ID and Region are required.");
        }
        var quotas = _quotaService.GetQuotas(subscriptionId, region);

        var response = new QuotaResponse()
        {
            SubscriptionId = subscriptionId,
            Region = region,
            Quotas = quotas
        };
        
        return Ok(response);
    }
    
    // Second Endpoint for seeking only Quota for specific Name
    [HttpGet("{subscriptionId}/regions/{region}/quotas/{quotaName}")]
    public IActionResult GetQuota(string subscriptionId, string region, string quotaName)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(region) ||
            string.IsNullOrWhiteSpace(quotaName))
        {
            return BadRequest("Subscription ID, Region and Quota Name are required");
        }
        var quota = _quotaService.GetQuota(subscriptionId, region, quotaName);

        if (quota == null)
        {
            return NotFound();
        }
        
        return Ok(quota);
    }
}

// ControllerBase gives us methodes like
// Ok(); BadRequest(); NotFound(); Unauthorized();