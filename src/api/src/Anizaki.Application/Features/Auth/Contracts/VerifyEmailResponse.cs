namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record VerifyEmailResponse(
    bool Verified,
    DateTime VerifiedAtUtc,
    string Email);

