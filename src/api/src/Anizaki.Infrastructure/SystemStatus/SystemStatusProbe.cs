using Anizaki.Application.Features.SystemStatus.Contracts;

namespace Anizaki.Infrastructure.SystemStatus;

public sealed class SystemStatusProbe : ISystemStatusProbe
{
    public Task<SystemStatusResponse> ProbeAsync(string? correlationId, CancellationToken cancellationToken = default)
    {
        var response = new SystemStatusResponse(
            Status: "healthy",
            CheckedAtUtc: DateTimeOffset.UtcNow,
            CorrelationId: correlationId);

        return Task.FromResult(response);
    }
}
