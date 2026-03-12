using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.Library.Contracts;
using Anizaki.Application.Features.SystemStatus.Contracts;
using Anizaki.Infrastructure.Auth;
using Anizaki.Infrastructure.Library;
using Anizaki.Infrastructure.SystemStatus;
using Microsoft.Extensions.DependencyInjection;

namespace Anizaki.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IUserTokenRepository, InMemoryUserTokenRepository>();
        services.AddSingleton<IAuthSessionRepository, InMemoryAuthSessionRepository>();
        services.AddSingleton<ILoginAttemptLockoutService, InMemoryLoginAttemptLockoutService>();
        services.AddSingleton<ISecurityAuditLogger, LoggerSecurityAuditLogger>();
        services.AddScoped<ICredentialHasher, Sha256CredentialHasher>();
        services.AddScoped<IAuthTokenService, BasicAuthTokenService>();
        services.AddScoped<IEmailSender, NoOpEmailSender>();
        services.AddScoped<ICurrentUserContext, UnauthenticatedCurrentUserContext>();
        services.AddSingleton<ISystemStatusProbe, SystemStatusProbe>();
        services.AddSingleton<ILibraryRepository, InMemoryLibraryRepository>();

        return services;
    }
}
