using Anizaki.Domain.Abstractions;
using Anizaki.Domain.Exceptions;

namespace Anizaki.Domain.Users;

public sealed class EmailVerificationToken : Entity<Guid>
{
    private EmailVerificationToken(
        Guid id,
        Guid userId,
        TokenHash tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime? usedAtUtc)
        : base(id)
    {
        UserId = EnsureNonEmpty(userId, nameof(userId));
        TokenHash = tokenHash;
        CreatedAtUtc = EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        ExpiresAtUtc = EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (ExpiresAtUtc <= CreatedAtUtc)
        {
            throw new DomainException("Email verification token expiry must be after creation.");
        }

        UsedAtUtc = usedAtUtc is null ? null : EnsureUtc(usedAtUtc.Value, nameof(usedAtUtc));
    }

    public Guid UserId { get; }

    public TokenHash TokenHash { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime ExpiresAtUtc { get; }

    public DateTime? UsedAtUtc { get; }

    public bool IsUsed => UsedAtUtc.HasValue;

    public static EmailVerificationToken Issue(
        Guid id,
        Guid userId,
        TokenHash tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        EnsureNonEmpty(id, nameof(id));
        return new EmailVerificationToken(id, userId, tokenHash, createdAtUtc, expiresAtUtc, null);
    }

    public bool IsExpired(DateTime asOfUtc)
    {
        var utc = EnsureUtc(asOfUtc, nameof(asOfUtc));
        return utc >= ExpiresAtUtc;
    }

    public bool CanBeUsedAt(DateTime asOfUtc)
    {
        return !IsUsed && !IsExpired(asOfUtc);
    }

    public EmailVerificationToken MarkUsed(DateTime usedAtUtc)
    {
        var utc = EnsureUtc(usedAtUtc, nameof(usedAtUtc));
        if (!CanBeUsedAt(utc))
        {
            throw new DomainException("Email verification token is not usable.");
        }

        return new EmailVerificationToken(Id, UserId, TokenHash, CreatedAtUtc, ExpiresAtUtc, utc);
    }

    private static Guid EnsureNonEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"Email verification token '{paramName}' cannot be empty.");
        }

        return value;
    }

    private static DateTime EnsureUtc(DateTime value, string paramName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"Email verification token '{paramName}' must be UTC.");
        }

        return value;
    }
}

