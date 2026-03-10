namespace Anizaki.Application.Features.SystemStatus.Contracts;

public interface ISystemStatusProbe
{
    Task<SystemStatusResponse> ProbeAsync(string? correlationId, CancellationToken cancellationToken = default);
}
