using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.Users;
using Anizaki.Application.Features.Users.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Tests.Features.Users;

public sealed class UpdateUserRoleHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithUnauthenticatedContext_ShouldThrowValidationException()
    {
        var handler = new UpdateUserRoleHandler(
            new UpdateUserRoleCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: false, userId: null, role: null),
            new InMemoryUserRepository(),
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(new UpdateUserRoleCommand(Guid.NewGuid(), "seller")));

        Assert.Contains(exception.Errors, error => error.Code == "auth.unauthenticated");
    }

    [Fact]
    public async Task HandleAsync_WithNonAdminContext_ShouldThrowValidationException()
    {
        var handler = new UpdateUserRoleHandler(
            new UpdateUserRoleCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid(), role: UserRole.User),
            new InMemoryUserRepository(),
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(new UpdateUserRoleCommand(Guid.NewGuid(), "seller")));

        Assert.Contains(exception.Errors, error => error.Code == "auth.forbidden");
    }

    [Fact]
    public async Task HandleAsync_WithUnknownUser_ShouldThrowValidationException()
    {
        var handler = new UpdateUserRoleHandler(
            new UpdateUserRoleCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid(), role: UserRole.Admin),
            new InMemoryUserRepository(),
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(new UpdateUserRoleCommand(Guid.NewGuid(), "seller")));

        Assert.Contains(exception.Errors, error => error.Code == "user.notFound");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidRole_ShouldThrowValidationException()
    {
        var repository = new InMemoryUserRepository();
        var targetUser = User.Create(Guid.NewGuid(), UserEmail.From("target@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddAsync(targetUser);

        var handler = new UpdateUserRoleHandler(
            new UpdateUserRoleCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid(), role: UserRole.Admin),
            repository,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(new UpdateUserRoleCommand(targetUser.Id, "operator")));

        Assert.Contains(exception.Errors, error => error.Code == "role.invalid");
    }

    [Fact]
    public async Task HandleAsync_WithUnverifiedTargetAndElevatedRole_ShouldThrowValidationException()
    {
        var repository = new InMemoryUserRepository();
        var targetUser = User.Create(Guid.NewGuid(), UserEmail.From("target@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddAsync(targetUser);

        var handler = new UpdateUserRoleHandler(
            new UpdateUserRoleCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid(), role: UserRole.Admin),
            repository,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(new UpdateUserRoleCommand(targetUser.Id, "seller")));

        Assert.Contains(exception.Errors, error => error.Code == "role.invalidTransition");
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldUpdateRole()
    {
        var repository = new InMemoryUserRepository();
        var auditLogger = new RecordingSecurityAuditLogger();
        var targetUser = User.Create(Guid.NewGuid(), UserEmail.From("target@example.com"), UserRole.User, DateTime.UtcNow);
        targetUser.MarkEmailVerified(DateTime.UtcNow.AddMinutes(1));
        await repository.AddAsync(targetUser);

        var handler = new UpdateUserRoleHandler(
            new UpdateUserRoleCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid(), role: UserRole.Admin),
            repository,
            auditLogger);

        var response = await handler.HandleAsync(new UpdateUserRoleCommand(targetUser.Id, "seller"));

        Assert.Equal(targetUser.Id, response.UserId);
        Assert.Equal("seller", response.Role);

        var persisted = await repository.GetByIdAsync(targetUser.Id);
        Assert.NotNull(persisted);
        Assert.Equal("seller", persisted!.Role.Value);
        Assert.Equal(1, auditLogger.UserRoleChangedCount);
    }

    private sealed class StubCurrentUserContext : ICurrentUserContext
    {
        public StubCurrentUserContext(bool isAuthenticated, Guid? userId, UserRole? role)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
            Role = role;
            Email = isAuthenticated ? UserEmail.From("admin@example.com") : null;
        }

        public bool IsAuthenticated { get; }

        public Guid? UserId { get; }

        public UserRole? Role { get; }

        public UserEmail? Email { get; }
    }

    private sealed class RecordingSecurityAuditLogger : ISecurityAuditLogger
    {
        public int UserRoleChangedCount { get; private set; }

        public Task LoginFailedAsync(
            string email,
            DateTime occurredAtUtc,
            bool accountLocked,
            DateTime? lockedUntilUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PasswordResetAsync(
            Guid userId,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EmailVerifiedAsync(
            Guid userId,
            string email,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UserRoleChangedAsync(
            Guid actorUserId,
            Guid targetUserId,
            string previousRole,
            string nextRole,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            UserRoleChangedCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _usersById = new();
        private readonly Dictionary<Guid, string> _passwordHashes = new();

        public Task<bool> ExistsByEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_usersById.Values.Any(u => u.Email == email));
        }

        public Task<User?> GetByEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
        {
            var user = _usersById.Values.SingleOrDefault(u => u.Email == email);
            return Task.FromResult(user);
        }

        public Task<UserCredentialSnapshot?> GetCredentialSnapshotByEmailAsync(
            UserEmail email,
            CancellationToken cancellationToken = default)
        {
            var user = _usersById.Values.SingleOrDefault(u => u.Email == email);
            if (user is null || !_passwordHashes.TryGetValue(user.Id, out var hash))
            {
                return Task.FromResult<UserCredentialSnapshot?>(null);
            }

            return Task.FromResult<UserCredentialSnapshot?>(new UserCredentialSnapshot(user, hash));
        }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task AddWithCredentialAsync(User user, string passwordHash, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            _passwordHashes[user.Id] = passwordHash;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            _passwordHashes[userId] = passwordHash;
            return Task.CompletedTask;
        }
    }
}
