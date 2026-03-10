using System.Collections.Concurrent;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Infrastructure.Auth;

public sealed class InMemoryAuthSessionRepository : IAuthSessionRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, AuthSessionRecord>> _sessionsByUserId = new();

    public Task AddOrUpdateAsync(
        AuthSessionRecord session,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessions = _sessionsByUserId.GetOrAdd(session.UserId, _ => new ConcurrentDictionary<Guid, AuthSessionRecord>());
        sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuthSessionRecord>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessionsByUserId.TryGetValue(userId, out var sessions))
        {
            return Task.FromResult<IReadOnlyCollection<AuthSessionRecord>>(Array.Empty<AuthSessionRecord>());
        }

        return Task.FromResult<IReadOnlyCollection<AuthSessionRecord>>(sessions.Values.ToArray());
    }

    public Task<AuthSessionRecord?> GetBySessionIdAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessionsByUserId.TryGetValue(userId, out var sessions))
        {
            return Task.FromResult<AuthSessionRecord?>(null);
        }

        sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<AuthSessionRecord?> GetByRefreshTokenHashAsync(
        Guid userId,
        TokenHash refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessionsByUserId.TryGetValue(userId, out var sessions))
        {
            return Task.FromResult<AuthSessionRecord?>(null);
        }

        var session = sessions.Values.SingleOrDefault(existing => existing.RefreshTokenHash == refreshTokenHash);
        return Task.FromResult(session);
    }
}

