namespace Anizaki.Application.Exceptions;

public sealed class RequestThrottledException : Exception
{
    public RequestThrottledException(string message, DateTime? retryAfterUtc = null)
        : base(message)
    {
        RetryAfterUtc = retryAfterUtc;
    }

    public DateTime? RetryAfterUtc { get; }
}
