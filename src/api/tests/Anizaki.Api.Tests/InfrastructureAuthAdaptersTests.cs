using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;
using Anizaki.Infrastructure.Auth;

namespace Anizaki.Api.Tests;

public class InfrastructureAuthAdaptersTests
{
    [Fact]
    public void IssueSessionTokens_ShouldGenerateOpaqueTokensAndExpectedExpiryWindow()
    {
        var nowUtc = DateTime.UtcNow;
        var user = User.Create(Guid.NewGuid(), UserEmail.From("session@example.com"), UserRole.User, nowUtc);
        var service = CreateService();

        var tokens = service.IssueSessionTokens(user, nowUtc);

        Assert.Equal(64, tokens.AccessToken.Length);
        Assert.Equal(64, tokens.RefreshToken.Length);
        Assert.NotEqual(tokens.AccessToken, tokens.RefreshToken);
        Assert.Equal(nowUtc.AddMinutes(15), tokens.AccessTokenExpiresAtUtc);
        Assert.Equal(nowUtc.AddDays(14), tokens.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public void IssueEmailVerificationToken_ShouldReturnHashForIssuedPlainTextToken()
    {
        var nowUtc = DateTime.UtcNow;
        var service = CreateService();

        var issue = service.IssueEmailVerificationToken(nowUtc);
        var expectedHash = service.HashOneTimeToken(issue.PlainTextToken);

        Assert.Equal(expectedHash, issue.TokenHash);
        Assert.Equal(nowUtc.AddHours(24), issue.ExpiresAtUtc);
    }

    [Fact]
    public void IssuePasswordResetToken_ShouldReturnHashForIssuedPlainTextToken()
    {
        var nowUtc = DateTime.UtcNow;
        var service = CreateService();

        var issue = service.IssuePasswordResetToken(nowUtc);
        var expectedHash = service.HashOneTimeToken(issue.PlainTextToken);

        Assert.Equal(expectedHash, issue.TokenHash);
        Assert.Equal(nowUtc.AddMinutes(30), issue.ExpiresAtUtc);
    }

    [Fact]
    public async Task RevokeSessionAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = CreateService();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.RevokeSessionAsync(Guid.NewGuid(), refreshToken: null, DateTime.UtcNow, cancellation.Token));
    }

    [Fact]
    public async Task SessionPrototypeOperations_ShouldSupportListRevokeAndSuspiciousFlag()
    {
        var nowUtc = DateTime.UtcNow;
        var user = User.Create(Guid.NewGuid(), UserEmail.From("session-proto@example.com"), UserRole.User, nowUtc);
        var service = CreateService();
        var tokens = service.IssueSessionTokens(user, nowUtc);

        await service.RecordSessionAsync(user.Id, tokens, nowUtc);

        var sessions = await service.ListSessionsAsync(user.Id);
        var session = Assert.Single(sessions);
        Assert.False(session.IsSuspicious);
        Assert.Null(session.RevokedAtUtc);

        var flagged = await service.MarkSessionSuspiciousAsync(
            user.Id,
            session.SessionId,
            "unexpected_device",
            nowUtc.AddMinutes(1));
        Assert.True(flagged);

        var revoked = await service.RevokeSessionByIdAsync(
            user.Id,
            session.SessionId,
            "manual_revoke",
            nowUtc.AddMinutes(2));
        Assert.True(revoked);

        var updated = Assert.Single(await service.ListSessionsAsync(user.Id));
        Assert.True(updated.IsSuspicious);
        Assert.NotNull(updated.SuspiciousMarkedAtUtc);
        Assert.NotNull(updated.RevokedAtUtc);
    }

    [Fact]
    public async Task NoOpEmailSender_ShouldCompleteAndHonorCancellation()
    {
        var sender = new NoOpEmailSender();
        var message = new AuthEmailMessage(
            UserEmail.From("notify@example.com"),
            Subject: "test",
            TextBody: "body",
            HtmlBody: null);

        await sender.SendAsync(message);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sender.SendAsync(message, cancellation.Token));
    }

    private static BasicAuthTokenService CreateService()
    {
        return new BasicAuthTokenService(new InMemoryAuthSessionRepository());
    }
}
