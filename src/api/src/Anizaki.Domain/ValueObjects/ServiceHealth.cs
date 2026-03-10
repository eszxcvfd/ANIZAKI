using Anizaki.Domain.Exceptions;

namespace Anizaki.Domain.ValueObjects;

public sealed class ServiceHealth : Abstractions.ValueObject
{
    private ServiceHealth(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ServiceHealth FromExternal(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainException("Service health status cannot be empty.");
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "healthy" => new ServiceHealth(normalized),
            "degraded" => new ServiceHealth(normalized),
            "unhealthy" => new ServiceHealth(normalized),
            _ => throw new DomainException($"Unsupported service health status: {status}.")
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}
