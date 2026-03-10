namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record RevokeMySessionResponse(bool Revoked, Guid SessionId);

