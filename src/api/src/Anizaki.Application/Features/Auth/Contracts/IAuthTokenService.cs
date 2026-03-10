using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public interface IAuthTokenService
{
    AuthSessionTokens IssueSessionTokens(User user, DateTime issuedAtUtc);

    Task RecordSessionAsync(
        Guid userId,
        AuthSessionTokens tokens,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken = default);

    OneTimeTokenIssue IssueEmailVerificationToken(DateTime issuedAtUtc);

    OneTimeTokenIssue IssuePasswordResetToken(DateTime issuedAtUtc);

    TokenHash HashOneTimeToken(string plainToken);

    Task RevokeSessionAsync(
        Guid userId,
        string? refreshToken,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuthSessionRecord>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionByIdAsync(
        Guid userId,
        Guid sessionId,
        string? reason,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> MarkSessionSuspiciousAsync(
        Guid userId,
        Guid sessionId,
        string reason,
        DateTime markedAtUtc,
        CancellationToken cancellationToken = default);
}
