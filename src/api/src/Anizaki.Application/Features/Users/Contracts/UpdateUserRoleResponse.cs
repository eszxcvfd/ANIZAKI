namespace Anizaki.Application.Features.Users.Contracts;

public sealed record UpdateUserRoleResponse(
    Guid UserId,
    string Role,
    DateTime UpdatedAtUtc);
