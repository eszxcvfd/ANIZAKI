namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record AuthSessionTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

