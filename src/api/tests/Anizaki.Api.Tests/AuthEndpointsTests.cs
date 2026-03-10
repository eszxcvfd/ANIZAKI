using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Anizaki.Api.Auth;
using Anizaki.Application.Features.Auth.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Anizaki.Api.Tests;

public sealed class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterAndLoginEndpoints_ShouldReturnExpectedPayload()
    {
        var httpClient = _factory.CreateClient();
        var email = $"auth-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(registerPayload);
        Assert.Equal(email, registerPayload!.Email);
        Assert.True(registerPayload.VerificationRequired);

        var loginResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        Assert.NotNull(loginPayload);
        Assert.Equal(email, loginPayload!.Email);
        Assert.Equal("user", loginPayload.Role);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginPayload.Tokens.RefreshToken));
    }

    [Fact]
    public async Task LoginEndpoint_WithInvalidCredentials_ShouldReturnValidationErrorEnvelope()
    {
        var httpClient = _factory.CreateClient();
        var email = $"invalid-login-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });

        var loginResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        var payload = await loginResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("validation_failed", payload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.Message));
        Assert.False(string.IsNullOrWhiteSpace(payload.CorrelationId));
    }

    [Fact]
    public async Task LoginEndpoint_WithRepeatedInvalidCredentials_ShouldReturnTooManyRequests()
    {
        var httpClient = _factory.CreateClient();
        var email = $"locked-login-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email,
                password = $"WrongPassword-{attempt}"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var lockedResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "WrongPassword-final"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, lockedResponse.StatusCode);
        Assert.True(lockedResponse.Headers.RetryAfter is not null);
        var payload = await lockedResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("too_many_requests", payload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.CorrelationId));
    }

    [Fact]
    public async Task LogoutEndpoint_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var httpClient = _factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken = "refresh-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("unauthorized", payload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.Message));
        Assert.False(string.IsNullOrWhiteSpace(payload.CorrelationId));
    }

    [Fact]
    public async Task LogoutEndpoint_WithAuthenticatedHeaders_ShouldReturnSuccess()
    {
        var httpClient = _factory.CreateClient();
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(registerPayload);

        AttachAuthenticatedHeaders(httpClient, registerPayload!.UserId, registerPayload.Email, "user");

        var logoutResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken = "refresh-token"
        });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        var payload = await logoutResponse.Content.ReadFromJsonAsync<LogoutResponsePayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.Revoked);
    }

    [Fact]
    public async Task SessionEndpoints_WithAuthenticatedHeaders_ShouldListRevokeAndFlagSuspicious()
    {
        var httpClient = _factory.CreateClient();
        var email = $"sessions-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(registerPayload);

        var loginResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        Assert.NotNull(loginPayload);

        AttachAuthenticatedHeaders(httpClient, registerPayload!.UserId, registerPayload.Email, "user");

        var getSessionsResponse = await httpClient.GetAsync("/api/v1/auth/sessions");
        Assert.Equal(HttpStatusCode.OK, getSessionsResponse.StatusCode);
        var listPayload = await getSessionsResponse.Content.ReadFromJsonAsync<GetMySessionsResponsePayload>();
        Assert.NotNull(listPayload);
        var session = Assert.Single(listPayload!.Sessions);

        var flagResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/auth/sessions/{session.SessionId}/flag-suspicious",
            new { reason = "unexpected_device" });
        Assert.Equal(HttpStatusCode.OK, flagResponse.StatusCode);
        var flagPayload = await flagResponse.Content.ReadFromJsonAsync<MarkSessionSuspiciousResponsePayload>();
        Assert.NotNull(flagPayload);
        Assert.True(flagPayload!.Flagged);

        var revokeResponse = await httpClient.PostAsJsonAsync(
            $"/api/v1/auth/sessions/{session.SessionId}/revoke",
            new { reason = "manual_revoke" });
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
        var revokePayload = await revokeResponse.Content.ReadFromJsonAsync<RevokeMySessionResponsePayload>();
        Assert.NotNull(revokePayload);
        Assert.True(revokePayload!.Revoked);
    }

    [Fact]
    public async Task SessionEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var httpClient = _factory.CreateClient();

        var listResponse = await httpClient.GetAsync("/api/v1/auth/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);

        var revokeResponse = await httpClient.PostAsJsonAsync($"/api/v1/auth/sessions/{Guid.NewGuid():D}/revoke", new { reason = "manual" });
        Assert.Equal(HttpStatusCode.Unauthorized, revokeResponse.StatusCode);

        var flagResponse = await httpClient.PostAsJsonAsync($"/api/v1/auth/sessions/{Guid.NewGuid():D}/flag-suspicious", new { reason = "unexpected_device" });
        Assert.Equal(HttpStatusCode.Unauthorized, flagResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordEndpoint_ShouldAlwaysReturnAccepted()
    {
        var httpClient = _factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            email = "unknown@example.com"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ForgotPasswordResponsePayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.Accepted);
    }

    [Fact]
    public async Task ForgotPasswordEndpoint_WhenRateLimitExceeded_ShouldReturnTooManyRequests()
    {
        var httpClient = _factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Test-RateLimit-Key", Guid.NewGuid().ToString("N"));

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var acceptedResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", new
            {
                email = $"throttle-{attempt}-{Guid.NewGuid():N}@example.com"
            });

            Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);
        }

        var throttledResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            email = $"throttle-final-{Guid.NewGuid():N}@example.com"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, throttledResponse.StatusCode);
        var payload = await throttledResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("too_many_requests", payload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.CorrelationId));
    }

    [Fact]
    public async Task ResetAndVerifyEndpoints_WithInvalidToken_ShouldReturnBadRequest()
    {
        var httpClient = _factory.CreateClient();

        var resetResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token = "missing-token",
            newPassword = "NewPassword123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, resetResponse.StatusCode);
        var resetPayload = await resetResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(resetPayload);
        Assert.Equal("validation_failed", resetPayload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(resetPayload.Message));
        Assert.False(string.IsNullOrWhiteSpace(resetPayload.CorrelationId));

        var verifyResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            token = "missing-token"
        });
        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(verifyPayload);
        Assert.Equal("validation_failed", verifyPayload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(verifyPayload.CorrelationId));
    }

    [Fact]
    public async Task UsersMeEndpoint_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var httpClient = _factory.CreateClient();

        var response = await httpClient.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UsersMeEndpoints_WithAuthenticatedHeaders_ShouldSupportGetAndUpdate()
    {
        var httpClient = _factory.CreateClient();
        var email = $"profile-{Guid.NewGuid():N}@example.com";
        var updatedEmail = $"profile-updated-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(registerPayload);

        AttachAuthenticatedHeaders(httpClient, registerPayload!.UserId, registerPayload.Email, "user");

        var getProfileResponse = await httpClient.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.OK, getProfileResponse.StatusCode);
        var getPayload = await getProfileResponse.Content.ReadFromJsonAsync<MyProfileResponsePayload>();
        Assert.NotNull(getPayload);
        Assert.Equal(email, getPayload!.Email);
        Assert.Equal("user", getPayload.Role);

        var updateProfileResponse = await httpClient.PutAsJsonAsync("/api/v1/users/me", new
        {
            email = updatedEmail
        });
        Assert.Equal(HttpStatusCode.OK, updateProfileResponse.StatusCode);
        var updatePayload = await updateProfileResponse.Content.ReadFromJsonAsync<MyProfileResponsePayload>();
        Assert.NotNull(updatePayload);
        Assert.Equal(updatedEmail, updatePayload!.Email);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("seller")]
    [InlineData("admin")]
    public async Task UsersMeEndpoint_WithAuthenticatedRole_ShouldAllowAccess(string role)
    {
        var httpClient = _factory.CreateClient();
        var email = $"role-{role}-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(registerPayload);

        AttachAuthenticatedHeaders(httpClient, registerPayload!.UserId, registerPayload.Email, role);

        var response = await httpClient.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UsersMeUpdate_WithInvalidEmail_ShouldReturnValidationErrorEnvelope()
    {
        var httpClient = _factory.CreateClient();
        var email = $"profile-invalid-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password
        });
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(registerPayload);

        AttachAuthenticatedHeaders(httpClient, registerPayload!.UserId, registerPayload.Email, "user");

        var response = await httpClient.PutAsJsonAsync("/api/v1/users/me", new
        {
            email = "invalid-email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("validation_failed", payload!.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.CorrelationId));
    }

    [Fact]
    public async Task AdminRoleEndpoint_WithNonAdminRole_ShouldReturnForbidden()
    {
        var httpClient = _factory.CreateClient();
        var targetEmail = $"target-{Guid.NewGuid():N}@example.com";
        var targetPassword = "Password123!";
        var targetRegisterResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = targetEmail,
            password = targetPassword
        });
        var targetPayload = await targetRegisterResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(targetPayload);

        AttachAuthenticatedHeaders(httpClient, Guid.NewGuid(), "actor@example.com", "user");

        var response = await httpClient.PutAsJsonAsync($"/api/v1/admin/users/{targetPayload!.UserId}/role", new
        {
            role = "seller"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("forbidden", payload!.Error);
    }

    [Fact]
    public async Task AdminRoleEndpoint_WithSellerRole_ShouldReturnForbidden()
    {
        var httpClient = _factory.CreateClient();
        var targetEmail = $"seller-target-{Guid.NewGuid():N}@example.com";
        var targetPassword = "Password123!";
        var targetRegisterResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = targetEmail,
            password = targetPassword
        });
        var targetPayload = await targetRegisterResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(targetPayload);

        AttachAuthenticatedHeaders(httpClient, Guid.NewGuid(), "seller@example.com", "seller");

        var response = await httpClient.PutAsJsonAsync($"/api/v1/admin/users/{targetPayload!.UserId}/role", new
        {
            role = "seller"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(payload);
        Assert.Equal("forbidden", payload!.Error);
    }

    [Fact]
    public async Task AdminRoleEndpoint_WithAdminRole_ShouldUpdateTargetRole()
    {
        var httpClient = _factory.CreateClient();
        var targetEmail = $"target-{Guid.NewGuid():N}@example.com";
        var targetPassword = "Password123!";
        var targetRegisterResponse = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = targetEmail,
            password = targetPassword
        });
        var targetPayload = await targetRegisterResponse.Content.ReadFromJsonAsync<RegisterResponsePayload>();
        Assert.NotNull(targetPayload);

        using (var scope = _factory.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var targetUser = await repository.GetByIdAsync(targetPayload!.UserId);
            Assert.NotNull(targetUser);
            targetUser!.MarkEmailVerified(DateTime.UtcNow);
            await repository.UpdateAsync(targetUser);
        }

        AttachAuthenticatedHeaders(httpClient, Guid.NewGuid(), "admin@example.com", "admin");

        var response = await httpClient.PutAsJsonAsync($"/api/v1/admin/users/{targetPayload!.UserId}/role", new
        {
            role = "seller"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UpdateUserRoleResponsePayload>();
        Assert.NotNull(payload);
        Assert.Equal(targetPayload.UserId, payload!.UserId);
        Assert.Equal("seller", payload.Role);
    }

    private static void AttachAuthenticatedHeaders(HttpClient httpClient, Guid userId, string email, string role)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "dev-token");

        if (httpClient.DefaultRequestHeaders.Contains(AuthenticationDefaults.UserIdHeader))
        {
            httpClient.DefaultRequestHeaders.Remove(AuthenticationDefaults.UserIdHeader);
        }

        if (httpClient.DefaultRequestHeaders.Contains(AuthenticationDefaults.EmailHeader))
        {
            httpClient.DefaultRequestHeaders.Remove(AuthenticationDefaults.EmailHeader);
        }

        if (httpClient.DefaultRequestHeaders.Contains(AuthenticationDefaults.RoleHeader))
        {
            httpClient.DefaultRequestHeaders.Remove(AuthenticationDefaults.RoleHeader);
        }

        httpClient.DefaultRequestHeaders.Add(AuthenticationDefaults.UserIdHeader, userId.ToString());
        httpClient.DefaultRequestHeaders.Add(AuthenticationDefaults.EmailHeader, email);
        httpClient.DefaultRequestHeaders.Add(AuthenticationDefaults.RoleHeader, role);
    }

    private sealed record RegisterResponsePayload(
        Guid UserId,
        string Email,
        bool VerificationRequired,
        DateTime VerificationTokenExpiresAtUtc);

    private sealed record LoginResponsePayload(
        Guid UserId,
        string Email,
        string Role,
        SessionTokensPayload Tokens);

    private sealed record SessionTokensPayload(
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc);

    private sealed record ForgotPasswordResponsePayload(bool Accepted);

    private sealed record MyProfileResponsePayload(
        Guid UserId,
        string Email,
        string Role,
        bool EmailVerified,
        DateTime? EmailVerifiedAtUtc,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    private sealed record ApiErrorPayload(
        string Error,
        string Message,
        string CorrelationId);

    private sealed record UpdateUserRoleResponsePayload(
        Guid UserId,
        string Role,
        DateTime UpdatedAtUtc);

    private sealed record LogoutResponsePayload(bool Revoked);

    private sealed record RevokeMySessionResponsePayload(bool Revoked, Guid SessionId);

    private sealed record MarkSessionSuspiciousResponsePayload(bool Flagged, Guid SessionId);

    private sealed record GetMySessionsResponsePayload(IReadOnlyCollection<AuthSessionPayload> Sessions);

    private sealed record AuthSessionPayload(
        Guid SessionId,
        DateTime IssuedAtUtc,
        DateTime AccessTokenExpiresAtUtc,
        DateTime RefreshTokenExpiresAtUtc,
        bool Revoked,
        DateTime? RevokedAtUtc,
        string? RevocationReason,
        bool Suspicious,
        DateTime? SuspiciousMarkedAtUtc,
        string? SuspiciousReason);
}
