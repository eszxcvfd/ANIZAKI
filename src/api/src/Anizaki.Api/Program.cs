using Anizaki.Api.Auth;
using Anizaki.Api.Errors;
using Anizaki.Application;
using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.SystemStatus.Contracts;
using Anizaki.Application.Features.Users.Contracts;
using Anizaki.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AuthenticationDefaults.Scheme;
        options.DefaultChallengeScheme = AuthenticationDefaults.Scheme;
    })
    .AddScheme<AuthenticationSchemeOptions, DevHeaderBearerAuthenticationHandler>(
        AuthenticationDefaults.Scheme,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser().RequireRole(
            AuthorizationPolicies.UserRole,
            AuthorizationPolicies.SellerRole,
            AuthorizationPolicies.AdminRole));

    options.AddPolicy(AuthorizationPolicies.SellerOrAdmin, policy =>
        policy.RequireAuthenticatedUser().RequireRole(
            AuthorizationPolicies.SellerRole,
            AuthorizationPolicies.AdminRole));

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.AdminRole));
});

const string loginRateLimitPolicy = "auth-login-rate-limit";
const string forgotPasswordRateLimitPolicy = "auth-forgot-password-rate-limit";
const string resetPasswordRateLimitPolicy = "auth-reset-password-rate-limit";
var loginRateLimitPermit = Math.Max(1, builder.Configuration.GetValue("Security:Auth:RateLimit:Login:PermitLimit", 20));
var loginRateLimitWindowSeconds = Math.Max(1, builder.Configuration.GetValue("Security:Auth:RateLimit:Login:WindowSeconds", 60));
var forgotPasswordRateLimitPermit = Math.Max(1, builder.Configuration.GetValue("Security:Auth:RateLimit:ForgotPassword:PermitLimit", 10));
var forgotPasswordRateLimitWindowSeconds = Math.Max(1, builder.Configuration.GetValue("Security:Auth:RateLimit:ForgotPassword:WindowSeconds", 60));
var resetPasswordRateLimitPermit = Math.Max(1, builder.Configuration.GetValue("Security:Auth:RateLimit:ResetPassword:PermitLimit", 10));
var resetPasswordRateLimitWindowSeconds = Math.Max(1, builder.Configuration.GetValue("Security:Auth:RateLimit:ResetPassword:WindowSeconds", 60));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";
        var correlationId = CorrelationIdResolver.Resolve(context.HttpContext);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        await response.WriteAsJsonAsync(new ApiErrorEnvelope(
            Error: "too_many_requests",
            Message: "Too many requests. Please try again later.",
            CorrelationId: correlationId), cancellationToken: cancellationToken);
    };

    options.AddPolicy(loginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-login:{ResolveClientRateLimitKey(httpContext)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginRateLimitPermit,
                Window = TimeSpan.FromSeconds(loginRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(forgotPasswordRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-forgot-password:{ResolveClientRateLimitKey(httpContext)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = forgotPasswordRateLimitPermit,
                Window = TimeSpan.FromSeconds(forgotPasswordRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy(resetPasswordRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-reset-password:{ResolveClientRateLimitKey(httpContext)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = resetPasswordRateLimitPermit,
                Window = TimeSpan.FromSeconds(resetPasswordRateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var correlationId = CorrelationIdResolver.Resolve(context);

        if (exception is RequestThrottledException throttledException)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (throttledException.RetryAfterUtc is not null)
            {
                var remainingSeconds = Math.Max(
                    1,
                    (int)Math.Ceiling((throttledException.RetryAfterUtc.Value - DateTime.UtcNow).TotalSeconds));
                context.Response.Headers.RetryAfter = remainingSeconds.ToString(CultureInfo.InvariantCulture);
            }

            await context.Response.WriteAsJsonAsync(new ApiErrorEnvelope(
                Error: "too_many_requests",
                Message: throttledException.Message,
                CorrelationId: correlationId));
            return;
        }

        if (exception is RequestValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorEnvelope(
                Error: "validation_failed",
                Message: "Request validation failed.",
                CorrelationId: correlationId,
                Errors: validationException.Errors));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ApiErrorEnvelope(
            Error: "internal_server_error",
            Message: "An unexpected error occurred.",
            CorrelationId: correlationId));
    });
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    checkedAtUtc = DateTimeOffset.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

var v1 = app.MapGroup("/api/v1");
var auth = v1.MapGroup("/auth");
var users = v1.MapGroup("/users");
var admin = v1.MapGroup("/admin").RequireAuthorization(AuthorizationPolicies.AdminOnly);

v1.MapGet("/system/status", async (
        string? correlationId,
        IRequestHandler<GetSystemStatusQuery, SystemStatusResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var query = new GetSystemStatusQuery(correlationId);
        var response = await handler.HandleAsync(query, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("GetSystemStatus")
    .WithOpenApi();

auth.MapPost("/register", async (
        RegisterUserCommand request,
        IRequestHandler<RegisterUserCommand, RegisterUserResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("RegisterUser")
    .WithOpenApi();

auth.MapPost("/login", async (
        LoginCommand request,
        IRequestHandler<LoginCommand, LoginResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("Login")
    .RequireRateLimiting(loginRateLimitPolicy)
    .WithOpenApi();

auth.MapPost("/logout", async (
        LogoutCommand request,
        IRequestHandler<LogoutCommand, LogoutResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
    .WithName("Logout")
    .WithOpenApi();

auth.MapGet("/sessions", async (
        IRequestHandler<GetMySessionsQuery, GetMySessionsResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(new GetMySessionsQuery(), cancellationToken);
        return Results.Ok(response);
    })
    .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
    .WithName("GetMySessions")
    .WithOpenApi();

auth.MapPost("/sessions/{sessionId:guid}/revoke", async (
        Guid sessionId,
        RevokeMySessionRequest request,
        IRequestHandler<RevokeMySessionCommand, RevokeMySessionResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(
            new RevokeMySessionCommand(sessionId, request.Reason),
            cancellationToken);
        return Results.Ok(response);
    })
    .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
    .WithName("RevokeMySession")
    .WithOpenApi();

auth.MapPost("/sessions/{sessionId:guid}/flag-suspicious", async (
        Guid sessionId,
        MarkSessionSuspiciousRequest request,
        IRequestHandler<MarkSessionSuspiciousCommand, MarkSessionSuspiciousResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(
            new MarkSessionSuspiciousCommand(sessionId, request.Reason),
            cancellationToken);
        return Results.Ok(response);
    })
    .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
    .WithName("MarkSessionSuspicious")
    .WithOpenApi();

auth.MapPost("/forgot-password", async (
        ForgotPasswordCommand request,
        IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("ForgotPassword")
    .RequireRateLimiting(forgotPasswordRateLimitPolicy)
    .WithOpenApi();

auth.MapPost("/reset-password", async (
        ResetPasswordCommand request,
        IRequestHandler<ResetPasswordCommand, ResetPasswordResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("ResetPassword")
    .RequireRateLimiting(resetPasswordRateLimitPolicy)
    .WithOpenApi();

auth.MapPost("/verify-email", async (
        VerifyEmailCommand request,
        IRequestHandler<VerifyEmailCommand, VerifyEmailResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("VerifyEmail")
    .WithOpenApi();

users.MapGet("/me", async (
        IRequestHandler<GetMyProfileQuery, MyProfileResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(new GetMyProfileQuery(), cancellationToken);
        return Results.Ok(response);
    })
    .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
    .WithName("GetMyProfile")
    .WithOpenApi();

users.MapPut("/me", async (
        UpdateMyProfileCommand request,
        IRequestHandler<UpdateMyProfileCommand, MyProfileResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
    .WithName("UpdateMyProfile")
    .WithOpenApi();

admin.MapPut("/users/{id:guid}/role", async (
        Guid id,
        UpdateUserRoleRequest request,
        IRequestHandler<UpdateUserRoleCommand, UpdateUserRoleResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var response = await handler.HandleAsync(new UpdateUserRoleCommand(id, request.Role), cancellationToken);
        return Results.Ok(response);
    })
    .WithName("UpdateUserRole")
    .WithOpenApi();

static string ResolveClientRateLimitKey(HttpContext httpContext)
{
    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
    var hostEnvironment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
    if (hostEnvironment.IsDevelopment() &&
        httpContext.Request.Headers.TryGetValue("X-Test-RateLimit-Key", out var testRateLimitKey) &&
        !string.IsNullOrWhiteSpace(testRateLimitKey))
    {
        return $"{remoteIp}:{testRateLimitKey}";
    }

    return remoteIp;
}

app.Run();

public partial class Program;

internal sealed record UpdateUserRoleRequest(string Role);
internal sealed record RevokeMySessionRequest(string? Reason);
internal sealed record MarkSessionSuspiciousRequest(string Reason);
