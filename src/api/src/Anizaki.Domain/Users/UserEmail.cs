using System.Net.Mail;
using Anizaki.Domain.Abstractions;
using Anizaki.Domain.Exceptions;

namespace Anizaki.Domain.Users;

public sealed class UserEmail : ValueObject
{
    private UserEmail(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static UserEmail From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("User email cannot be empty.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!IsValid(normalized))
        {
            throw new DomainException($"Invalid user email format: {value}.");
        }

        return new UserEmail(normalized);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    private static bool IsValid(string value)
    {
        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public override string ToString() => Value;
}

