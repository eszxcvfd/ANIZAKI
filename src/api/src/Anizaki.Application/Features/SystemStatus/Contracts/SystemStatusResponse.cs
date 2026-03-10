namespace Anizaki.Application.Features.SystemStatus.Contracts;

public sealed record SystemStatusResponse(
    string Status,
    DateTimeOffset CheckedAtUtc,
    string? CorrelationId);
