namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record AuthSessionSummary(
    Guid SessionId,
    DateTime IssuedAtUtc,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    bool Revoked,
    DateTime? RevokedAtUtc,
    string? RevocationReason,
    bool Suspicious,
    DateTime? SuspiciousMarkedAtUtc,
    string? SuspiciousReason);

