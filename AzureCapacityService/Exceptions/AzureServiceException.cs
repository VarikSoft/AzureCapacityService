namespace AzureCapacityService.Exceptions;

public class AzureServiceException : Exception
{
    public int StatusCode { get; }

    public AzureServiceException(
        string message,
        int statusCode,
        Exception? innerException = null) // I used here innetException, so I could see not only my message but also the initiate one
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}