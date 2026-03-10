using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Domain.Tests.Users;

public class UserStateTransitionsTests
{
    [Fact]
    public void VerifyEmail_ShouldConsumeTokenAndMarkUserVerified()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);
        var token = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            user.Id,
            TokenHash.From(new string('v', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(24));

        var usedToken = user.VerifyEmail(token, createdAtUtc.AddMinutes(3));

        Assert.True(user.IsEmailVerified);
        Assert.True(usedToken.IsUsed);
    }

    [Fact]
    public void VerifyEmail_ShouldRejectTokenFromDifferentUser()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);
        var token = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('w', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(24));

        Assert.Throws<DomainException>(() => user.VerifyEmail(token, createdAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void VerifyEmail_ShouldRejectWhenAlreadyVerified()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);
        var tokenA = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            user.Id,
            TokenHash.From(new string('x', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(24));
        var tokenB = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            user.Id,
            TokenHash.From(new string('y', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(24));

        user.VerifyEmail(tokenA, createdAtUtc.AddMinutes(1));
        Assert.Throws<DomainException>(() => user.VerifyEmail(tokenB, createdAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void ApplyPasswordReset_ShouldConsumeTokenAndUpdatePasswordTimestamp()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);
        var token = PasswordResetToken.Issue(
            Guid.NewGuid(),
            user.Id,
            TokenHash.From(new string('z', 64)),
            createdAtUtc,
            createdAtUtc.AddMinutes(30));

        var usedToken = user.ApplyPasswordReset(token, createdAtUtc.AddMinutes(5));

        Assert.True(usedToken.IsUsed);
        Assert.Equal(createdAtUtc.AddMinutes(5), user.PasswordChangedAtUtc);
    }

    [Fact]
    public void ApplyPasswordReset_ShouldRejectTokenFromDifferentUser()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);
        var token = PasswordResetToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('q', 64)),
            createdAtUtc,
            createdAtUtc.AddMinutes(30));

        Assert.Throws<DomainException>(() => user.ApplyPasswordReset(token, createdAtUtc.AddMinutes(5)));
    }

    [Fact]
    public void ChangeRole_ShouldRejectNoOpTransition()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);

        Assert.Throws<DomainException>(() => user.ChangeRole(UserRole.User, createdAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void ChangeRole_ShouldRejectElevatedRoleWhenEmailIsUnverified()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);

        Assert.Throws<DomainException>(() => user.ChangeRole(UserRole.Seller, createdAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void ChangeRole_ShouldAllowElevatedRoleWhenEmailIsVerified()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = NewUser(createdAtUtc);
        var token = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            user.Id,
            TokenHash.From(new string('m', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(12));

        user.VerifyEmail(token, createdAtUtc.AddMinutes(1));
        user.ChangeRole(UserRole.Seller, createdAtUtc.AddMinutes(2));

        Assert.Equal("seller", user.Role.Value);
    }

    private static User NewUser(DateTime createdAtUtc)
    {
        return User.Create(
            Guid.NewGuid(),
            UserEmail.From("state@example.com"),
            UserRole.User,
            createdAtUtc);
    }
}

