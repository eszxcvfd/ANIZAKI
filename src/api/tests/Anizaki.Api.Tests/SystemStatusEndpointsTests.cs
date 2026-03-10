using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Anizaki.Api.Tests;

public class SystemStatusEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;

    public SystemStatusEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        var response = await _httpClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
        Assert.NotNull(payload);
        Assert.Equal("healthy", payload.Status);
    }

    [Fact]
    public async Task VersionedSystemStatusEndpoint_ShouldReturnNormalizedStatusAndCorrelationId()
    {
        var response = await _httpClient.GetAsync("/api/v1/system/status?correlationId=e2e-smoke");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SystemStatusPayload>();
        Assert.NotNull(payload);
        Assert.Equal("healthy", payload.Status);
        Assert.Equal("e2e-smoke", payload.CorrelationId);
    }

    private sealed record HealthPayload(string Status, DateTimeOffset CheckedAtUtc);

    private sealed record SystemStatusPayload(string Status, DateTimeOffset CheckedAtUtc, string? CorrelationId);
}
