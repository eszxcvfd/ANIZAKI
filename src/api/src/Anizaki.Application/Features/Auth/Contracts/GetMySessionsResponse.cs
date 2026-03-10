namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record GetMySessionsResponse(IReadOnlyCollection<AuthSessionSummary> Sessions);

