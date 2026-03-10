using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.Users;
using Anizaki.Application.Features.Users.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Tests.Features.Users;

public class ProfileHandlersTests
{
    [Fact]
    public async Task GetMyProfileHandler_WithUnauthenticatedContext_ShouldThrowValidationException()
    {
        var handler = new GetMyProfileHandler(
            new GetMyProfileQueryValidator(),
            new StubCurrentUserContext(isAuthenticated: false, userId: null),
            new InMemoryUserRepository());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetMyProfileQuery()));

        Assert.Contains(exception.Errors, error => error.Code == "auth.unauthenticated");
    }

    [Fact]
    public async Task GetMyProfileHandler_WithAuthenticatedContext_ShouldReturnProfile()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryUserRepository();
        var user = User.Create(userId, UserEmail.From("profile@example.com"), UserRole.Seller, DateTime.UtcNow);
        await repository.AddAsync(user);
        var handler = new GetMyProfileHandler(
            new GetMyProfileQueryValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            repository);

        var response = await handler.HandleAsync(new GetMyProfileQuery());

        Assert.Equal(userId, response.UserId);
        Assert.Equal("profile@example.com", response.Email);
        Assert.Equal("seller", response.Role);
    }

    [Fact]
    public async Task GetMyProfileHandler_WithMissingUser_ShouldThrowValidationException()
    {
        var handler = new GetMyProfileHandler(
            new GetMyProfileQueryValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid()),
            new InMemoryUserRepository());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetMyProfileQuery()));

        Assert.Contains(exception.Errors, error => error.Code == "auth.userNotFound");
    }

    [Fact]
    public async Task UpdateMyProfileHandler_WithInvalidEmailFormat_ShouldThrowValidationException()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryUserRepository();
        await repository.AddAsync(User.Create(userId, UserEmail.From("profile@example.com"), UserRole.User, DateTime.UtcNow));
        var handler = new UpdateMyProfileHandler(
            new UpdateMyProfileCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            repository);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new UpdateMyProfileCommand("invalid-email")));

        Assert.Contains(exception.Errors, error => error.Code == "email.invalid");
    }

    [Fact]
    public async Task UpdateMyProfileHandler_WithNewEmail_ShouldUpdateEmailAndResetVerification()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryUserRepository();
        var user = User.Create(userId, UserEmail.From("old@example.com"), UserRole.User, DateTime.UtcNow);
        user.MarkEmailVerified(DateTime.UtcNow.AddMinutes(1));
        await repository.AddAsync(user);
        var handler = new UpdateMyProfileHandler(
            new UpdateMyProfileCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            repository);

        var response = await handler.HandleAsync(new UpdateMyProfileCommand("new@example.com"));

        Assert.Equal("new@example.com", response.Email);
        Assert.False(response.EmailVerified);
        var persistedUser = await repository.GetByIdAsync(userId);
        Assert.NotNull(persistedUser);
        Assert.Equal("new@example.com", persistedUser!.Email.Value);
        Assert.False(persistedUser.IsEmailVerified);
    }

    [Fact]
    public async Task UpdateMyProfileHandler_WithUnauthenticatedContext_ShouldThrowValidationException()
    {
        var handler = new UpdateMyProfileHandler(
            new UpdateMyProfileCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: false, userId: null),
            new InMemoryUserRepository());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new UpdateMyProfileCommand("profile@example.com")));

        Assert.Contains(exception.Errors, error => error.Code == "auth.unauthenticated");
    }

    [Fact]
    public async Task UpdateMyProfileHandler_WithMissingUser_ShouldThrowValidationException()
    {
        var handler = new UpdateMyProfileHandler(
            new UpdateMyProfileCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: Guid.NewGuid()),
            new InMemoryUserRepository());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new UpdateMyProfileCommand("profile@example.com")));

        Assert.Contains(exception.Errors, error => error.Code == "auth.userNotFound");
    }

    [Fact]
    public async Task UpdateMyProfileHandler_WithSameEmail_ShouldPreserveVerifiedState()
    {
        var userId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;
        var repository = new InMemoryUserRepository();
        var user = User.Create(userId, UserEmail.From("same@example.com"), UserRole.User, nowUtc);
        user.MarkEmailVerified(nowUtc.AddMinutes(1));
        await repository.AddAsync(user);
        var handler = new UpdateMyProfileHandler(
            new UpdateMyProfileCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            repository);

        var response = await handler.HandleAsync(new UpdateMyProfileCommand("same@example.com"));

        Assert.Equal("same@example.com", response.Email);
        Assert.True(response.EmailVerified);
        Assert.Equal(nowUtc.AddMinutes(1), response.EmailVerifiedAtUtc);
    }

    [Fact]
    public void UpdateMyProfileValidator_WithMissingEmail_ShouldReturnError()
    {
        var validator = new UpdateMyProfileCommandValidator();
        var result = validator.Validate(new UpdateMyProfileCommand(" "));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("email.empty", error.Code);
    }

    private sealed class StubCurrentUserContext : ICurrentUserContext
    {
        public StubCurrentUserContext(bool isAuthenticated, Guid? userId)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
            Role = isAuthenticated ? UserRole.User : null;
            Email = isAuthenticated ? UserEmail.From("current@example.com") : null;
        }

        public bool IsAuthenticated { get; }

        public Guid? UserId { get; }

        public UserRole? Role { get; }

        public UserEmail? Email { get; }
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
