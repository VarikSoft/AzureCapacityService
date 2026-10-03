using Microsoft.AspNetCore.Diagnostics;

using AzureCapacityService.Models;

namespace AzureCapacityService.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Check that this is our Azure exception
        if (exception is not AzureServiceException azureException)
        {
            return false;
        }

        // 2. Map Azure status code to our HTTP status code
        var statusCode = azureException.StatusCode switch
        {
            400 => StatusCodes.Status400BadRequest,
            401 => StatusCodes.Status401Unauthorized,
            403 => StatusCodes.Status403Forbidden,
            404 => StatusCodes.Status404NotFound,
            429 => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status502BadGateway
        };

        // 3. Set response status
        httpContext.Response.StatusCode = statusCode;

        // 4. Write JSON response
        var errorResponse = new ErrorResponse
        {
            Error = "AzureServiceError",
            Message = "Azure service request failed."
        };
        
        _logger.LogError(
            azureException,
            "Azure service request failed. Path: {Path}, AzureStatusCode: {AzureStatusCode}, ResponseStatusCode: {ResponseStatusCode}",
            httpContext.Request.Path,
            azureException.StatusCode,
            statusCode);

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        
        // 5. Tell ASP.NET that exception was handled
        return true;
    }
}