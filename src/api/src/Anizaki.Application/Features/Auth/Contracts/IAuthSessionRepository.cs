using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public interface IAuthSessionRepository
{
    Task AddOrUpdateAsync(
        AuthSessionRecord session,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuthSessionRecord>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AuthSessionRecord?> GetBySessionIdAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<AuthSessionRecord?> GetByRefreshTokenHashAsync(
        Guid userId,
        TokenHash refreshTokenHash,
        CancellationToken cancellationToken = default);
}

