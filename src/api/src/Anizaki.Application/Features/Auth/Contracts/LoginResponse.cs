namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string Role,
    AuthSessionTokens Tokens);

