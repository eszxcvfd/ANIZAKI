using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record AuthSessionRecord(
    Guid SessionId,
    Guid UserId,
    TokenHash RefreshTokenHash,
    DateTime IssuedAtUtc,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    DateTime? RevokedAtUtc,
    string? RevocationReason,
    bool IsSuspicious,
    DateTime? SuspiciousMarkedAtUtc,
    string? SuspiciousReason);

