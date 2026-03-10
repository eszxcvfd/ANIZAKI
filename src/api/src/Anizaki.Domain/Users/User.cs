using Anizaki.Domain.Abstractions;
using Anizaki.Domain.Exceptions;

namespace Anizaki.Domain.Users;

public sealed class User : Entity<Guid>, IAggregateRoot
{
    private User(
        Guid id,
        UserEmail email,
        UserRole role,
        DateTime createdAtUtc)
        : base(id)
    {
        Email = email;
        Role = role;
        CreatedAtUtc = EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
    }

    public UserEmail Email { get; private set; }

    public UserRole Role { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? EmailVerifiedAtUtc { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAtUtc.HasValue;

    public DateTime? PasswordChangedAtUtc { get; private set; }

    public static User Create(
        Guid id,
        UserEmail email,
        UserRole role,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("User id cannot be empty.");
        }

        return new User(id, email, role, createdAtUtc);
    }

    public void ChangeRole(UserRole newRole, DateTime changedAtUtc)
    {
        if (Role == newRole)
        {
            throw new DomainException("User role is already set to the requested value.");
        }

        if (!IsEmailVerified && newRole != UserRole.User)
        {
            throw new DomainException("Cannot assign elevated roles to an unverified user.");
        }

        Role = newRole;
        UpdatedAtUtc = EnsureUtc(changedAtUtc, nameof(changedAtUtc));
    }

    public void ChangeEmail(UserEmail newEmail, DateTime changedAtUtc)
    {
        Email = newEmail;
        EmailVerifiedAtUtc = null;
        UpdatedAtUtc = EnsureUtc(changedAtUtc, nameof(changedAtUtc));
    }

    public void MarkEmailVerified(DateTime verifiedAtUtc)
    {
        if (IsEmailVerified)
        {
            throw new DomainException("User email is already verified.");
        }

        EmailVerifiedAtUtc = EnsureUtc(verifiedAtUtc, nameof(verifiedAtUtc));
        UpdatedAtUtc = EmailVerifiedAtUtc.Value;
    }

    public EmailVerificationToken VerifyEmail(
        EmailVerificationToken token,
        DateTime verifiedAtUtc)
    {
        if (token.UserId != Id)
        {
            throw new DomainException("Email verification token does not belong to this user.");
        }

        var usedToken = token.MarkUsed(verifiedAtUtc);
        MarkEmailVerified(verifiedAtUtc);
        return usedToken;
    }

    public PasswordResetToken ApplyPasswordReset(
        PasswordResetToken token,
        DateTime resetAtUtc)
    {
        if (token.UserId != Id)
        {
            throw new DomainException("Password reset token does not belong to this user.");
        }

        var usedToken = token.MarkUsed(resetAtUtc);
        var utc = EnsureUtc(resetAtUtc, nameof(resetAtUtc));
        PasswordChangedAtUtc = utc;
        UpdatedAtUtc = utc;
        return usedToken;
    }

    private static DateTime EnsureUtc(DateTime value, string paramName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"User timestamp '{paramName}' must be UTC.");
        }

        return value;
    }
}
