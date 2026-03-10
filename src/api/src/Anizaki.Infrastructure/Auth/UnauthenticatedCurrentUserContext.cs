using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Infrastructure.Auth;

public sealed class UnauthenticatedCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public UserRole? Role => null;

    public UserEmail? Email => null;
}

