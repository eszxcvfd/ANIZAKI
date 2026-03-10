using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Domain.Tests.Users;

public class UserTokenPrimitivesTests
{
    [Fact]
    public void TokenHash_ShouldRejectShortValues()
    {
        Assert.Throws<DomainException>(() => TokenHash.From("too-short"));
    }

    [Fact]
    public void EmailVerificationToken_ShouldBeUsableBeforeExpiry()
    {
        var createdAtUtc = DateTime.UtcNow;
        var token = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('a', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(24));

        Assert.True(token.CanBeUsedAt(createdAtUtc.AddMinutes(1)));
        Assert.False(token.IsUsed);
    }

    [Fact]
    public void EmailVerificationToken_MarkUsed_ShouldReturnNewUsedInstance()
    {
        var createdAtUtc = DateTime.UtcNow;
        var token = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('b', 64)),
            createdAtUtc,
            createdAtUtc.AddHours(24));

        var used = token.MarkUsed(createdAtUtc.AddMinutes(5));

        Assert.False(token.IsUsed);
        Assert.True(used.IsUsed);
        Assert.NotNull(used.UsedAtUtc);
    }

    [Fact]
    public void EmailVerificationToken_ShouldRejectUseWhenExpired()
    {
        var createdAtUtc = DateTime.UtcNow;
        var token = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('c', 64)),
            createdAtUtc,
            createdAtUtc.AddMinutes(30));

        Assert.Throws<DomainException>(() => token.MarkUsed(createdAtUtc.AddMinutes(31)));
    }

    [Fact]
    public void PasswordResetToken_ShouldRejectDoubleUse()
    {
        var createdAtUtc = DateTime.UtcNow;
        var token = PasswordResetToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('d', 64)),
            createdAtUtc,
            createdAtUtc.AddMinutes(30));

        var used = token.MarkUsed(createdAtUtc.AddMinutes(5));

        Assert.Throws<DomainException>(() => used.MarkUsed(createdAtUtc.AddMinutes(6)));
    }

    [Fact]
    public void PasswordResetToken_ShouldRequireUtcTimestamps()
    {
        var createdAtUtc = DateTime.UtcNow;
        var token = PasswordResetToken.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TokenHash.From(new string('e', 64)),
            createdAtUtc,
            createdAtUtc.AddMinutes(30));

        Assert.Throws<DomainException>(() => token.IsExpired(DateTime.Now));
    }
}

