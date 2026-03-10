namespace Anizaki.Application.Features.Users.Contracts;

public sealed record MyProfileResponse(
    Guid UserId,
    string Email,
    string Role,
    bool EmailVerified,
    DateTime? EmailVerifiedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

