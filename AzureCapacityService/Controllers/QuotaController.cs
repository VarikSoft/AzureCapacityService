
using AzureCapacityService.Models;
using AzureCapacityService.Options;
using AzureCapacityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureCapacityService.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class QuotaController : ControllerBase
{
    private readonly IQuotaService _quotaService;
    private readonly AzureOptions _azureOptions;

    public QuotaController(
        IQuotaService quotaService,
        IOptions<AzureOptions> azureOptions)
    {
        _quotaService = quotaService;
        _azureOptions = azureOptions.Value;
    }

    // Uses the default subscription and region from configuration.
    [HttpGet("quotas")]
    public Task<IActionResult> GetDefaultQuotasAsync(
        CancellationToken cancellationToken)
    {
        return GetConfiguredQuotasAsync(
            _azureOptions.DefaultRegion,
            cancellationToken);
    }

    // Uses the default subscription with a custom region.
    [HttpGet("regions/{region}/quotas")]
    public Task<IActionResult> GetRegionQuotasAsync(
        string region,
        CancellationToken cancellationToken)
    {
        return GetConfiguredQuotasAsync(
            region,
            cancellationToken);
    }

    // Uses an explicitly specified subscription and region.
    [HttpGet("subscriptions/{subscriptionId}/regions/{region}/quotas")]
    [ProducesResponseType(typeof(QuotaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetQuotasAsync(
        string subscriptionId,
        string region,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId) ||
            string.IsNullOrWhiteSpace(region))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "ValidationError",
                Message = "Subscription ID and Region are required."
            });
        }

        var quotas = await _quotaService.GetQuotasAsync(
            subscriptionId,
            region,
            cancellationToken);

        return Ok(new QuotaResponse
        {
            SubscriptionId = subscriptionId,
            Region = region,
            Quotas = quotas
        });
    }

    // Gets a specific quota from an explicitly specified subscription.
    [HttpGet("subscriptions/{subscriptionId}/regions/{region}/quotas/{quotaName}")]
    [ProducesResponseType(typeof(QuotaInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetQuotaAsync(
        string subscriptionId,
        string region,
        string quotaName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId) ||
            string.IsNullOrWhiteSpace(region) ||
            string.IsNullOrWhiteSpace(quotaName))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "ValidationError",
                Message = "Subscription ID, Region and Quota Name are required."
            });
        }

        var quota = await _quotaService.GetQuotaAsync(
            subscriptionId,
            region,
            quotaName,
            cancellationToken);

        if (quota is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "QuotaNotFound",
                Message = $"Quota '{quotaName}' was not found."
            });
        }

        return Ok(quota);
    }

    private Task<IActionResult> GetConfiguredQuotasAsync(
        string? region,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_azureOptions.SubscriptionId))
        {
            return Task.FromResult<IActionResult>(Problem(
                title: "Azure configuration error",
                detail: "Default subscription ID is not configured.",
                statusCode: StatusCodes.Status500InternalServerError));
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            return Task.FromResult<IActionResult>(Problem(
                title: "Azure configuration error",
                detail: "Azure region is not configured.",
                statusCode: StatusCodes.Status500InternalServerError));
        }

        return GetQuotasAsync(
            _azureOptions.SubscriptionId,
            region,
            cancellationToken);
    }
}