using Anizaki.Application;
using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.SystemStatus;
using Anizaki.Application.Features.SystemStatus.Contracts;
using Anizaki.Application.Features.Users;
using Anizaki.Application.Features.Users.Contracts;
using Anizaki.Infrastructure;
using Anizaki.Infrastructure.Auth;
using Anizaki.Infrastructure.SystemStatus;
using Microsoft.Extensions.DependencyInjection;

namespace Anizaki.Api.Tests;

public class DependencyInjectionRegistrationTests
{
    [Fact]
    public void AddApplication_ShouldRegisterSystemStatusUseCaseBoundaries()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestValidator<GetSystemStatusQuery>) &&
                descriptor.ImplementationType == typeof(GetSystemStatusQueryValidator));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<GetSystemStatusQuery, SystemStatusResponse>) &&
                descriptor.ImplementationType == typeof(GetSystemStatusHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestValidator<ForgotPasswordCommand>) &&
                descriptor.ImplementationType == typeof(ForgotPasswordCommandValidator));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestValidator<GetMySessionsQuery>) &&
                descriptor.ImplementationType == typeof(GetMySessionsQueryValidator));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<GetMySessionsQuery, GetMySessionsResponse>) &&
                descriptor.ImplementationType == typeof(GetMySessionsHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestValidator<RevokeMySessionCommand>) &&
                descriptor.ImplementationType == typeof(RevokeMySessionCommandValidator));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<RevokeMySessionCommand, RevokeMySessionResponse>) &&
                descriptor.ImplementationType == typeof(RevokeMySessionHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestValidator<MarkSessionSuspiciousCommand>) &&
                descriptor.ImplementationType == typeof(MarkSessionSuspiciousCommandValidator));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<MarkSessionSuspiciousCommand, MarkSessionSuspiciousResponse>) &&
                descriptor.ImplementationType == typeof(MarkSessionSuspiciousHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>) &&
                descriptor.ImplementationType == typeof(ResetPasswordHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>) &&
                descriptor.ImplementationType == typeof(VerifyEmailHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<GetMyProfileQuery, MyProfileResponse>) &&
                descriptor.ImplementationType == typeof(GetMyProfileHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<UpdateMyProfileCommand, MyProfileResponse>) &&
                descriptor.ImplementationType == typeof(UpdateMyProfileHandler));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestValidator<UpdateUserRoleCommand>) &&
                descriptor.ImplementationType == typeof(UpdateUserRoleCommandValidator));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRequestHandler<UpdateUserRoleCommand, UpdateUserRoleResponse>) &&
                descriptor.ImplementationType == typeof(UpdateUserRoleHandler));
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterSystemStatusProbeBoundary()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure();

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(ISystemStatusProbe) &&
                descriptor.ImplementationType == typeof(SystemStatusProbe));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IUserTokenRepository) &&
                descriptor.ImplementationType == typeof(InMemoryUserTokenRepository));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IAuthSessionRepository) &&
                descriptor.ImplementationType == typeof(InMemoryAuthSessionRepository));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(ICredentialHasher) &&
                descriptor.ImplementationType == typeof(Sha256CredentialHasher));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IAuthTokenService) &&
                descriptor.ImplementationType == typeof(BasicAuthTokenService));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(ILoginAttemptLockoutService) &&
                descriptor.ImplementationType == typeof(InMemoryLoginAttemptLockoutService));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(ISecurityAuditLogger) &&
                descriptor.ImplementationType == typeof(LoggerSecurityAuditLogger));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IEmailSender) &&
                descriptor.ImplementationType == typeof(NoOpEmailSender));
    }
}
