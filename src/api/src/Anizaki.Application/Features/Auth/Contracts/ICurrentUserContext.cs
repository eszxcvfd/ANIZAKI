using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    UserRole? Role { get; }

    UserEmail? Email { get; }
}

