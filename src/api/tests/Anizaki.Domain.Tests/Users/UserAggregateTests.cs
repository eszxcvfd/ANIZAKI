using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Domain.Tests.Users;

public class UserAggregateTests
{
    [Fact]
    public void UserEmail_ShouldNormalizeToLowercaseAndTrim()
    {
        var value = UserEmail.From("  USER@Example.COM ");

        Assert.Equal("user@example.com", value.Value);
    }

    [Fact]
    public void UserEmail_ShouldRejectInvalidFormat()
    {
        Assert.Throws<DomainException>(() => UserEmail.From("invalid-email"));
    }

    [Fact]
    public void UserRole_ShouldAcceptKnownRoles()
    {
        Assert.Equal("user", UserRole.From("USER").Value);
        Assert.Equal("seller", UserRole.From("seller").Value);
        Assert.Equal("admin", UserRole.From("Admin").Value);
    }

    [Fact]
    public void UserRole_ShouldRejectUnknownRole()
    {
        Assert.Throws<DomainException>(() => UserRole.From("support"));
    }

    [Fact]
    public void Create_ShouldInitializeAggregateState()
    {
        var createdAtUtc = DateTime.UtcNow;

        var user = User.Create(
            Guid.NewGuid(),
            UserEmail.From("person@example.com"),
            UserRole.User,
            createdAtUtc);

        Assert.Equal("person@example.com", user.Email.Value);
        Assert.Equal("user", user.Role.Value);
        Assert.False(user.IsEmailVerified);
        Assert.Equal(createdAtUtc, user.CreatedAtUtc);
        Assert.Equal(createdAtUtc, user.UpdatedAtUtc);
    }

    [Fact]
    public void Create_ShouldRequireNonEmptyIdentifier()
    {
        var createdAtUtc = DateTime.UtcNow;

        Assert.Throws<DomainException>(() => User.Create(
            Guid.Empty,
            UserEmail.From("person@example.com"),
            UserRole.User,
            createdAtUtc));
    }

    [Fact]
    public void Create_ShouldRequireUtcTimestamp()
    {
        var localCreatedAt = DateTime.Now;

        Assert.Throws<DomainException>(() => User.Create(
            Guid.NewGuid(),
            UserEmail.From("person@example.com"),
            UserRole.User,
            localCreatedAt));
    }

    [Fact]
    public void ChangeEmail_ShouldResetVerificationAndUpdateTimestamp()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = User.Create(
            Guid.NewGuid(),
            UserEmail.From("person@example.com"),
            UserRole.User,
            createdAtUtc);

        user.MarkEmailVerified(createdAtUtc.AddMinutes(5));
        user.ChangeEmail(UserEmail.From("next@example.com"), createdAtUtc.AddMinutes(10));

        Assert.Equal("next@example.com", user.Email.Value);
        Assert.False(user.IsEmailVerified);
        Assert.Equal(createdAtUtc.AddMinutes(10), user.UpdatedAtUtc);
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRoleAndTimestamp()
    {
        var createdAtUtc = DateTime.UtcNow;
        var user = User.Create(
            Guid.NewGuid(),
            UserEmail.From("person@example.com"),
            UserRole.User,
            createdAtUtc);

        user.MarkEmailVerified(createdAtUtc.AddMinutes(1));
        user.ChangeRole(UserRole.Admin, createdAtUtc.AddMinutes(3));

        Assert.Equal("admin", user.Role.Value);
        Assert.Equal(createdAtUtc.AddMinutes(3), user.UpdatedAtUtc);
    }
}
