using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.SystemStatus;
using Anizaki.Application.Features.SystemStatus.Contracts;

namespace Anizaki.Application.Tests.Features.SystemStatus;

public class GetSystemStatusHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidQuery_ShouldReturnProbeResult()
    {
        var expected = new SystemStatusResponse("HEALTHY", DateTimeOffset.UtcNow, "trace-123");
        var probe = new StubSystemStatusProbe(expected);
        var validator = new GetSystemStatusQueryValidator();
        var handler = new GetSystemStatusHandler(probe, validator);
        var query = new GetSystemStatusQuery("trace-123");

        var response = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Equal(expected.CheckedAtUtc, response.CheckedAtUtc);
        Assert.Equal(expected.CorrelationId, response.CorrelationId);
        Assert.Equal("healthy", response.Status);
        Assert.Equal("trace-123", probe.LastCorrelationId);
        Assert.Equal(1, probe.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidQuery_ShouldThrowValidationException()
    {
        var probe = new StubSystemStatusProbe(new SystemStatusResponse("healthy", DateTimeOffset.UtcNow, null));
        var validator = new GetSystemStatusQueryValidator();
        var handler = new GetSystemStatusHandler(probe, validator);
        var invalidQuery = new GetSystemStatusQuery("   ");

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => handler.HandleAsync(invalidQuery));

        var error = Assert.Single(exception.Errors);
        Assert.Equal("correlationId.empty", error.Code);
        Assert.Equal(0, probe.CallCount);
    }

    private sealed class StubSystemStatusProbe : ISystemStatusProbe
    {
        private readonly SystemStatusResponse _response;

        public StubSystemStatusProbe(SystemStatusResponse response)
        {
            _response = response;
        }

        public int CallCount { get; private set; }

        public string? LastCorrelationId { get; private set; }

        public Task<SystemStatusResponse> ProbeAsync(string? correlationId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastCorrelationId = correlationId;
            return Task.FromResult(_response);
        }
    }
}
