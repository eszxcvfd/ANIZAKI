using Anizaki.Api.Auth;
using Anizaki.Application.Features.Auth.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Anizaki.Api.Tests;

public sealed class AuthCompositionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IServiceProvider _services;

    public AuthCompositionTests(WebApplicationFactory<Program> factory)
    {
        _services = factory.Services;
    }

    [Fact]
    public async Task AuthenticationDefaults_ShouldUseAnizakiBearerScheme()
    {
        var schemeProvider = _services.GetRequiredService<IAuthenticationSchemeProvider>();

        var defaultAuthenticateScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        var defaultChallengeScheme = await schemeProvider.GetDefaultChallengeSchemeAsync();

        Assert.NotNull(defaultAuthenticateScheme);
        Assert.NotNull(defaultChallengeScheme);
        Assert.Equal(AuthenticationDefaults.Scheme, defaultAuthenticateScheme!.Name);
        Assert.Equal(AuthenticationDefaults.Scheme, defaultChallengeScheme!.Name);
    }

    [Fact]
    public void AuthorizationPolicies_ShouldBeRegistered()
    {
        var options = _services.GetRequiredService<IOptions<AuthorizationOptions>>();

        Assert.NotNull(options.Value.GetPolicy(AuthorizationPolicies.AuthenticatedUser));
        Assert.NotNull(options.Value.GetPolicy(AuthorizationPolicies.SellerOrAdmin));
        Assert.NotNull(options.Value.GetPolicy(AuthorizationPolicies.AdminOnly));
    }

    [Fact]
    public void CurrentUserContext_ShouldResolveToHttpContextImplementation()
    {
        using var scope = _services.CreateScope();

        var currentUserContext = scope.ServiceProvider.GetRequiredService<ICurrentUserContext>();

        Assert.IsType<HttpContextCurrentUserContext>(currentUserContext);
        Assert.False(currentUserContext.IsAuthenticated);
    }
}
