namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email,
    bool VerificationRequired,
    DateTime VerificationTokenExpiresAtUtc);

