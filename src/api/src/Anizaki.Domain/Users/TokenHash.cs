using Anizaki.Domain.Abstractions;
using Anizaki.Domain.Exceptions;

namespace Anizaki.Domain.Users;

public sealed class TokenHash : ValueObject
{
    private TokenHash(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TokenHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Token hash cannot be empty.");
        }

        var normalized = value.Trim();
        if (normalized.Length < 32)
        {
            throw new DomainException("Token hash must be at least 32 characters.");
        }

        return new TokenHash(normalized);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

