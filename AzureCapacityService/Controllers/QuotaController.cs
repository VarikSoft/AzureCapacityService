using AzureCapacityService.Models;
using AzureCapacityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureCapacityService.Controllers;

[ApiController] // Converts our class into API controller
[Route("api/subscriptions")] // defines a basic URL api/subscriptions
[Produces("application/json")]
public class QuotaController : ControllerBase
{
    private readonly IQuotaService _quotaService;

    public QuotaController(IQuotaService quotaService)
    {
        _quotaService = quotaService;
    }
    
    // First Endpoint to Get all quotas list
    [HttpGet("{subscriptionId}/regions/{region}/quotas")] // Means that method processes GET api/subscriptions/{subscriptionId}/regions/{region}/quotas
    [ProducesResponseType(typeof(QuotaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetQuotasAsync(string subscriptionId, string region, CancellationToken cancellationToken)
    {
        // Why IsNullOrWhiteSpace insted of IsNullOrEmpty, is that because IsNullOrWhiteSpace can catch strings like "    "
        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(region)) // I've added a primitive data validation
        {
            return BadRequest(new ErrorResponse
            {
                Error = "ValidationError",
                Message = "Subscription ID and Region are required."
            });
        }
        var quotas = await _quotaService.GetQuotasAsync(subscriptionId, region, cancellationToken);

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
    [ProducesResponseType(typeof(QuotaInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetQuotaAsync(string subscriptionId, string region, string quotaName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(region) ||
            string.IsNullOrWhiteSpace(quotaName))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "ValidationError",
                Message = "Subscription ID, Region and Quota Name are required."
            });
        }
        
        var quota = await _quotaService.GetQuotaAsync(subscriptionId, region, quotaName, cancellationToken);

        if (quota == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "QuotaNotFound",
                Message = $"Quota '{quotaName}' was not found."
            });
        }
        
        return Ok(quota);
    }
}

// ControllerBase gives us methods like
// Ok(); BadRequest(); NotFound(); Unauthorized();