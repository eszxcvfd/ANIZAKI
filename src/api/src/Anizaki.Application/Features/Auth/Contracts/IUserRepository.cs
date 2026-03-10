using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(UserEmail email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(UserEmail email, CancellationToken cancellationToken = default);

    Task<UserCredentialSnapshot?> GetCredentialSnapshotByEmailAsync(
        UserEmail email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task AddWithCredentialAsync(
        User user,
        string passwordHash,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task SetPasswordHashAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken = default);
}
