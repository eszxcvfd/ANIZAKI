using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Tests.Features.Auth;

public class AuthDependencyAbstractionsTests
{
    [Fact]
    public async Task UserRepositoryAbstraction_ShouldSupportPersistenceAndLookupByEmail()
    {
        var repository = new InMemoryUserRepository();
        var user = User.Create(
            Guid.NewGuid(),
            UserEmail.From("auth-user@example.com"),
            UserRole.User,
            DateTime.UtcNow);

        await repository.AddAsync(user);
        var exists = await repository.ExistsByEmailAsync(UserEmail.From("auth-user@example.com"));
        var loaded = await repository.GetByEmailAsync(UserEmail.From("auth-user@example.com"));

        Assert.True(exists);
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded!.Id);
    }

    [Fact]
    public void TokenServiceAbstraction_ShouldExposeSessionAndOneTimeTokenContracts()
    {
        var user = User.Create(
            Guid.NewGuid(),
            UserEmail.From("token@example.com"),
            UserRole.User,
            DateTime.UtcNow);
        var tokenService = new StubAuthTokenService();
        var nowUtc = DateTime.UtcNow;

        var sessionTokens = tokenService.IssueSessionTokens(user, nowUtc);
        var verifyToken = tokenService.IssueEmailVerificationToken(nowUtc);
        var resetToken = tokenService.IssuePasswordResetToken(nowUtc);

        Assert.False(string.IsNullOrWhiteSpace(sessionTokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(sessionTokens.RefreshToken));
        Assert.True(sessionTokens.AccessTokenExpiresAtUtc > nowUtc);
        Assert.True(sessionTokens.RefreshTokenExpiresAtUtc > nowUtc);
        Assert.False(string.IsNullOrWhiteSpace(verifyToken.PlainTextToken));
        Assert.False(string.IsNullOrWhiteSpace(resetToken.PlainTextToken));
    }

    [Fact]
    public async Task EmailSenderAndCurrentUserContextAbstractions_ShouldBeConsumableByApplicationFlow()
    {
        ICurrentUserContext context = new StubCurrentUserContext(
            userId: Guid.NewGuid(),
            role: UserRole.Seller,
            email: UserEmail.From("seller@example.com"));
        var sender = new RecordingEmailSender();

        await sender.SendAsync(new AuthEmailMessage(
            context.Email!,
            "Verify your account",
            "Please verify",
            null));

        Assert.True(context.IsAuthenticated);
        Assert.Equal("seller", context.Role!.Value);
        Assert.Equal("seller@example.com", sender.LastMessage?.Recipient.Value);
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

        public Task AddWithCredentialAsync(
            User user,
            string passwordHash,
            CancellationToken cancellationToken = default)
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

        public Task SetPasswordHashAsync(
            Guid userId,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            _passwordHashes[userId] = passwordHash;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAuthTokenService : IAuthTokenService
    {
        private readonly Dictionary<Guid, AuthSessionRecord> _sessionsById = new();

        public AuthSessionTokens IssueSessionTokens(User user, DateTime issuedAtUtc)
        {
            return new AuthSessionTokens(
                AccessToken: "access-token",
                AccessTokenExpiresAtUtc: issuedAtUtc.AddMinutes(15),
                RefreshToken: "refresh-token",
                RefreshTokenExpiresAtUtc: issuedAtUtc.AddDays(14));
        }

        public Task RecordSessionAsync(
            Guid userId,
            AuthSessionTokens tokens,
            DateTime issuedAtUtc,
            CancellationToken cancellationToken = default)
        {
            var sessionId = Guid.NewGuid();
            _sessionsById[sessionId] = new AuthSessionRecord(
                SessionId: sessionId,
                UserId: userId,
                RefreshTokenHash: HashOneTimeToken(tokens.RefreshToken),
                IssuedAtUtc: issuedAtUtc,
                AccessTokenExpiresAtUtc: tokens.AccessTokenExpiresAtUtc,
                RefreshTokenExpiresAtUtc: tokens.RefreshTokenExpiresAtUtc,
                RevokedAtUtc: null,
                RevocationReason: null,
                IsSuspicious: false,
                SuspiciousMarkedAtUtc: null,
                SuspiciousReason: null);
            return Task.CompletedTask;
        }

        public OneTimeTokenIssue IssueEmailVerificationToken(DateTime issuedAtUtc)
        {
            return new OneTimeTokenIssue(
                PlainTextToken: "verify-token",
                TokenHash: TokenHash.From(new string('a', 64)),
                ExpiresAtUtc: issuedAtUtc.AddHours(24));
        }

        public OneTimeTokenIssue IssuePasswordResetToken(DateTime issuedAtUtc)
        {
            return new OneTimeTokenIssue(
                PlainTextToken: "reset-token",
                TokenHash: TokenHash.From(new string('b', 64)),
                ExpiresAtUtc: issuedAtUtc.AddMinutes(30));
        }

        public TokenHash HashOneTimeToken(string plainToken)
        {
            return TokenHash.From(new string('c', 64));
        }

        public Task RevokeSessionAsync(
            Guid userId,
            string? refreshToken,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AuthSessionRecord>> ListSessionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var sessions = _sessionsById.Values.Where(session => session.UserId == userId).ToArray();
            return Task.FromResult<IReadOnlyCollection<AuthSessionRecord>>(sessions);
        }

        public Task<bool> RevokeSessionByIdAsync(
            Guid userId,
            Guid sessionId,
            string? reason,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> MarkSessionSuspiciousAsync(
            Guid userId,
            Guid sessionId,
            string reason,
            DateTime markedAtUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public AuthEmailMessage? LastMessage { get; private set; }

        public Task SendAsync(AuthEmailMessage message, CancellationToken cancellationToken = default)
        {
            LastMessage = message;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCurrentUserContext : ICurrentUserContext
    {
        public StubCurrentUserContext(Guid userId, UserRole role, UserEmail email)
        {
            UserId = userId;
            Role = role;
            Email = email;
        }

        public bool IsAuthenticated => true;

        public Guid? UserId { get; }

        public UserRole? Role { get; }

        public UserEmail? Email { get; }
    }
}
