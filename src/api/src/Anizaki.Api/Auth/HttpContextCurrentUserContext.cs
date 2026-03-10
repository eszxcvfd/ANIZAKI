using System.Security.Claims;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Api.Auth;

public sealed class HttpContextCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var userId) ? userId : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                return UserRole.From(raw);
            }
            catch (DomainException)
            {
                return null;
            }
        }
    }

    public UserEmail? Email
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                return UserEmail.From(raw);
            }
            catch (DomainException)
            {
                return null;
            }
        }
    }
}
