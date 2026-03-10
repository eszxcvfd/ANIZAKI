using System.Security.Cryptography;
using System.Text;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Infrastructure.Auth;

public sealed class BasicAuthTokenService : IAuthTokenService
{
    private readonly IAuthSessionRepository _sessionRepository;

    public BasicAuthTokenService(IAuthSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public AuthSessionTokens IssueSessionTokens(User user, DateTime issuedAtUtc)
    {
        var accessToken = GenerateOpaqueToken();
        var refreshToken = GenerateOpaqueToken();
        return new AuthSessionTokens(
            AccessToken: accessToken,
            AccessTokenExpiresAtUtc: issuedAtUtc.AddMinutes(15),
            RefreshToken: refreshToken,
            RefreshTokenExpiresAtUtc: issuedAtUtc.AddDays(14));
    }

    public async Task RecordSessionAsync(
        Guid userId,
        AuthSessionTokens tokens,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var refreshTokenHash = HashOneTimeToken(tokens.RefreshToken);
        await _sessionRepository.AddOrUpdateAsync(
            new AuthSessionRecord(
                SessionId: Guid.NewGuid(),
                UserId: userId,
                RefreshTokenHash: refreshTokenHash,
                IssuedAtUtc: issuedAtUtc,
                AccessTokenExpiresAtUtc: tokens.AccessTokenExpiresAtUtc,
                RefreshTokenExpiresAtUtc: tokens.RefreshTokenExpiresAtUtc,
                RevokedAtUtc: null,
                RevocationReason: null,
                IsSuspicious: false,
                SuspiciousMarkedAtUtc: null,
                SuspiciousReason: null),
            cancellationToken);
    }

    public OneTimeTokenIssue IssueEmailVerificationToken(DateTime issuedAtUtc)
    {
        var token = GenerateOpaqueToken();
        return new OneTimeTokenIssue(
            PlainTextToken: token,
            TokenHash: HashOneTimeToken(token),
            ExpiresAtUtc: issuedAtUtc.AddHours(24));
    }

    public OneTimeTokenIssue IssuePasswordResetToken(DateTime issuedAtUtc)
    {
        var token = GenerateOpaqueToken();
        return new OneTimeTokenIssue(
            PlainTextToken: token,
            TokenHash: HashOneTimeToken(token),
            ExpiresAtUtc: issuedAtUtc.AddMinutes(30));
    }

    public TokenHash HashOneTimeToken(string plainToken)
    {
        var bytes = Encoding.UTF8.GetBytes(plainToken);
        var hash = SHA256.HashData(bytes);
        return TokenHash.From(Convert.ToHexString(hash));
    }

    public Task RevokeSessionAsync(
        Guid userId,
        string? refreshToken,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Task.CompletedTask;
        }

        return RevokeByRefreshTokenAsync(userId, refreshToken, revokedAtUtc, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuthSessionRecord>> ListSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _sessionRepository.ListByUserAsync(userId, cancellationToken);
    }

    public async Task<bool> RevokeSessionByIdAsync(
        Guid userId,
        Guid sessionId,
        string? reason,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = await _sessionRepository.GetBySessionIdAsync(userId, sessionId, cancellationToken);
        if (current is null)
        {
            return false;
        }

        if (current.RevokedAtUtc.HasValue)
        {
            return true;
        }

        await _sessionRepository.AddOrUpdateAsync(
            current with
            {
                RevokedAtUtc = revokedAtUtc,
                RevocationReason = NormalizeReason(reason) ?? "manual_revoke"
            },
            cancellationToken);
        return true;
    }

    public async Task<bool> MarkSessionSuspiciousAsync(
        Guid userId,
        Guid sessionId,
        string reason,
        DateTime markedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = await _sessionRepository.GetBySessionIdAsync(userId, sessionId, cancellationToken);
        if (current is null)
        {
            return false;
        }

        await _sessionRepository.AddOrUpdateAsync(
            current with
            {
                IsSuspicious = true,
                SuspiciousMarkedAtUtc = markedAtUtc,
                SuspiciousReason = NormalizeReason(reason) ?? "suspicious_activity"
            },
            cancellationToken);
        return true;
    }

    private async Task RevokeByRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var hash = HashOneTimeToken(refreshToken);
        var current = await _sessionRepository.GetByRefreshTokenHashAsync(userId, hash, cancellationToken);
        if (current is null || current.RevokedAtUtc.HasValue)
        {
            return;
        }

        await _sessionRepository.AddOrUpdateAsync(
            current with
            {
                RevokedAtUtc = revokedAtUtc,
                RevocationReason = "logout"
            },
            cancellationToken);
    }

    private static string? NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string GenerateOpaqueToken()
    {
        var buffer = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(buffer);
    }
}
