using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Anizaki.Api.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Anizaki.Api.Auth;

public sealed class DevHeaderBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevHeaderBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(authorizationHeaderValues.ToString(), out var authorizationHeader) ||
            !string.Equals(authorizationHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorizationHeader.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid bearer token format."));
        }

        if (!TryGetRequiredHeader(AuthenticationDefaults.UserIdHeader, out var userIdRaw) ||
            !Guid.TryParse(userIdRaw, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid user id header."));
        }

        if (!TryGetRequiredHeader(AuthenticationDefaults.EmailHeader, out var email))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing user email header."));
        }

        if (!TryGetRequiredHeader(AuthenticationDefaults.RoleHeader, out var role))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing user role header."));
        }

        var normalizedRole = role.Trim().ToLowerInvariant();
        if (normalizedRole is not (AuthorizationPolicies.UserRole or AuthorizationPolicies.SellerRole or AuthorizationPolicies.AdminRole))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unsupported user role header."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, normalizedRole)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        var correlationId = CorrelationIdResolver.Resolve(Context);
        return Response.WriteAsJsonAsync(new ApiErrorEnvelope(
            Error: "unauthorized",
            Message: "Authentication is required to access this resource.",
            CorrelationId: correlationId));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        if (Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        var correlationId = CorrelationIdResolver.Resolve(Context);
        return Response.WriteAsJsonAsync(new ApiErrorEnvelope(
            Error: "forbidden",
            Message: "You do not have access to this resource.",
            CorrelationId: correlationId));
    }

    private bool TryGetRequiredHeader(string headerName, out string value)
    {
        if (Request.Headers.TryGetValue(headerName, out var values) && !string.IsNullOrWhiteSpace(values))
        {
            value = values.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }
}
