using System.Collections.Concurrent;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Infrastructure.Auth;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _usersById = new();
    private readonly ConcurrentDictionary<Guid, string> _passwordHashes = new();

    public Task<bool> ExistsByEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_usersById.Values.Any(user => user.Email == email));
    }

    public Task<User?> GetByEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
    {
        var user = _usersById.Values.SingleOrDefault(item => item.Email == email);
        return Task.FromResult(user);
    }

    public Task<UserCredentialSnapshot?> GetCredentialSnapshotByEmailAsync(
        UserEmail email,
        CancellationToken cancellationToken = default)
    {
        var user = _usersById.Values.SingleOrDefault(item => item.Email == email);
        if (user is null || !_passwordHashes.TryGetValue(user.Id, out var passwordHash))
        {
            return Task.FromResult<UserCredentialSnapshot?>(null);
        }

        return Task.FromResult<UserCredentialSnapshot?>(new UserCredentialSnapshot(user, passwordHash));
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _usersById.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        EnsureEmailAvailable(user);
        _usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task AddWithCredentialAsync(User user, string passwordHash, CancellationToken cancellationToken = default)
    {
        EnsureEmailAvailable(user);
        _usersById[user.Id] = user;
        _passwordHashes[user.Id] = passwordHash;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        EnsureEmailAvailable(user);
        _usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (!_usersById.ContainsKey(userId))
        {
            throw new InvalidOperationException("Cannot set password hash for a user that does not exist.");
        }

        _passwordHashes[userId] = passwordHash;
        return Task.CompletedTask;
    }

    private void EnsureEmailAvailable(User user)
    {
        var duplicateExists = _usersById.Values.Any(existing => existing.Id != user.Id && existing.Email == user.Email);
        if (duplicateExists)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }
    }
}
