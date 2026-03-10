namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record MarkSessionSuspiciousResponse(bool Flagged, Guid SessionId);

