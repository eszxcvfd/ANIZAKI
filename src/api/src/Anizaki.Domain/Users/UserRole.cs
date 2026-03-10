using Anizaki.Domain.Abstractions;
using Anizaki.Domain.Exceptions;

namespace Anizaki.Domain.Users;

public sealed class UserRole : ValueObject
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "user",
        "seller",
        "admin"
    };

    private UserRole(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static UserRole User => new("user");

    public static UserRole Seller => new("seller");

    public static UserRole Admin => new("admin");

    public static UserRole From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("User role cannot be empty.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!AllowedRoles.Contains(normalized))
        {
            throw new DomainException($"Unsupported user role: {value}.");
        }

        return new UserRole(normalized);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

