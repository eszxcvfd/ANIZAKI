using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Tests.Features.Auth;

public class AuthHandlersTests
{
    [Fact]
    public async Task RegisterUserHandler_WithValidPayload_ShouldPersistUserAndSendVerification()
    {
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var hasher = new StubCredentialHasher();
        var tokenService = new StubTokenService();
        var emailSender = new RecordingEmailSender();
        var handler = new RegisterUserHandler(
            new RegisterUserCommandValidator(),
            repository,
            tokenRepository,
            hasher,
            tokenService,
            emailSender);

        var response = await handler.HandleAsync(new RegisterUserCommand("person@example.com", "Password123!"));

        Assert.True(response.VerificationRequired);
        Assert.True(await repository.ExistsByEmailAsync(UserEmail.From("person@example.com")));
        Assert.NotNull(emailSender.LastMessage);
        Assert.Equal(1, tokenService.EmailVerificationIssueCount);
        Assert.Equal(1, tokenRepository.EmailVerificationTokenCount);
    }

    [Fact]
    public async Task RegisterUserHandler_WithDuplicateEmail_ShouldThrowValidationException()
    {
        var repository = new InMemoryUserRepository();
        var existing = User.Create(Guid.NewGuid(), UserEmail.From("person@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddWithCredentialAsync(existing, "hash:seed");
        var handler = new RegisterUserHandler(
            new RegisterUserCommandValidator(),
            repository,
            new InMemoryUserTokenRepository(),
            new StubCredentialHasher(),
            new StubTokenService(),
            new RecordingEmailSender());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new RegisterUserCommand("person@example.com", "Password123!")));

        Assert.Contains(exception.Errors, error => error.Code == "email.duplicate");
    }

    [Fact]
    public async Task RegisterUserHandler_WithInvalidEmailFormat_ShouldThrowValidationException()
    {
        var handler = new RegisterUserHandler(
            new RegisterUserCommandValidator(),
            new InMemoryUserRepository(),
            new InMemoryUserTokenRepository(),
            new StubCredentialHasher(),
            new StubTokenService(),
            new RecordingEmailSender());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new RegisterUserCommand("invalid-email", "Password123!")));

        Assert.Contains(exception.Errors, error => error.Code == "email.invalid");
    }

    [Fact]
    public async Task LoginHandler_WithValidCredentials_ShouldReturnSessionTokens()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new StubCredentialHasher();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("login@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddWithCredentialAsync(user, hasher.HashPassword("Password123!"));
          var tokenService = new StubTokenService();
          var handler = new LoginHandler(
            new LoginCommandValidator(),
            repository,
            hasher,
            tokenService,
            new InMemoryLoginAttemptLockoutService(),
            new RecordingSecurityAuditLogger());

        var response = await handler.HandleAsync(new LoginCommand("login@example.com", "Password123!"));

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal("user", response.Role);
        Assert.Equal(1, tokenService.SessionIssueCount);
        Assert.Equal(1, tokenService.RecordedSessionCount);
    }

    [Fact]
    public async Task LoginHandler_WithInvalidCredentials_ShouldThrowValidationException()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new StubCredentialHasher();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("login@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddWithCredentialAsync(user, hasher.HashPassword("Password123!"));
          var handler = new LoginHandler(
            new LoginCommandValidator(),
            repository,
            hasher,
            new StubTokenService(),
            new InMemoryLoginAttemptLockoutService(),
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new LoginCommand("login@example.com", "WrongPassword")));

        Assert.Contains(exception.Errors, error => error.Code == "auth.invalidCredentials");
    }

    [Fact]
      public async Task LoginHandler_WithInvalidEmailFormat_ShouldThrowValidationException()
      {
          var handler = new LoginHandler(
            new LoginCommandValidator(),
            new InMemoryUserRepository(),
            new StubCredentialHasher(),
            new StubTokenService(),
            new InMemoryLoginAttemptLockoutService(),
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new LoginCommand("invalid-email", "Password123!")));

          Assert.Contains(exception.Errors, error => error.Code == "email.invalid");
      }

      [Fact]
      public async Task LoginHandler_WithRepeatedInvalidCredentials_ShouldThrowRequestThrottledException()
      {
          var repository = new InMemoryUserRepository();
          var hasher = new StubCredentialHasher();
          var user = User.Create(Guid.NewGuid(), UserEmail.From("login-throttle@example.com"), UserRole.User, DateTime.UtcNow);
          await repository.AddWithCredentialAsync(user, hasher.HashPassword("Password123!"));
          var handler = new LoginHandler(
            new LoginCommandValidator(),
            repository,
            hasher,
            new StubTokenService(),
            new InMemoryLoginAttemptLockoutService(),
            new RecordingSecurityAuditLogger());

          for (var attempt = 0; attempt < 4; attempt++)
          {
              await Assert.ThrowsAsync<RequestValidationException>(
                  () => handler.HandleAsync(new LoginCommand("login-throttle@example.com", $"WrongPassword-{attempt}")));
          }

          var throttledException = await Assert.ThrowsAsync<RequestThrottledException>(
              () => handler.HandleAsync(new LoginCommand("login-throttle@example.com", "WrongPassword-final")));

          Assert.NotNull(throttledException.RetryAfterUtc);
      }

    [Fact]
    public async Task LogoutHandler_WithUnauthenticatedContext_ShouldThrowValidationException()
    {
        var handler = new LogoutHandler(
            new LogoutCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: false, userId: null),
            new StubTokenService());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new LogoutCommand("refresh-token")));

        Assert.Contains(exception.Errors, error => error.Code == "auth.unauthenticated");
    }

    [Fact]
    public async Task LogoutHandler_WithAuthenticatedContext_ShouldRevokeSession()
    {
        var userId = Guid.NewGuid();
        var tokenService = new StubTokenService();
        var handler = new LogoutHandler(
            new LogoutCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            tokenService);

        var response = await handler.HandleAsync(new LogoutCommand("refresh-token"));

        Assert.True(response.Revoked);
        Assert.Equal(userId, tokenService.LastRevokedUserId);
        Assert.Equal("refresh-token", tokenService.LastRevokedRefreshToken);
    }

    [Fact]
    public async Task GetMySessionsHandler_WithAuthenticatedContext_ShouldReturnOrderedSessions()
    {
        var userId = Guid.NewGuid();
        var tokenService = new StubTokenService();
        var nowUtc = DateTime.UtcNow;

        await tokenService.RecordSessionAsync(
            userId,
            new AuthSessionTokens(
                AccessToken: "access-1",
                AccessTokenExpiresAtUtc: nowUtc.AddMinutes(15),
                RefreshToken: "refresh-1",
                RefreshTokenExpiresAtUtc: nowUtc.AddDays(14)),
            nowUtc.AddMinutes(-5));

        await tokenService.RecordSessionAsync(
            userId,
            new AuthSessionTokens(
                AccessToken: "access-2",
                AccessTokenExpiresAtUtc: nowUtc.AddMinutes(10),
                RefreshToken: "refresh-2",
                RefreshTokenExpiresAtUtc: nowUtc.AddDays(14)),
            nowUtc);

        var handler = new GetMySessionsHandler(
            new GetMySessionsQueryValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            tokenService);

        var response = await handler.HandleAsync(new GetMySessionsQuery());

        Assert.Equal(2, response.Sessions.Count);
        Assert.True(response.Sessions.First().IssuedAtUtc >= response.Sessions.Last().IssuedAtUtc);
    }

    [Fact]
    public async Task RevokeMySessionHandler_WhenSessionExists_ShouldReturnRevokedResponse()
    {
        var userId = Guid.NewGuid();
        var tokenService = new StubTokenService();
        var nowUtc = DateTime.UtcNow;
        await tokenService.RecordSessionAsync(
            userId,
            new AuthSessionTokens(
                AccessToken: "access-1",
                AccessTokenExpiresAtUtc: nowUtc.AddMinutes(15),
                RefreshToken: "refresh-1",
                RefreshTokenExpiresAtUtc: nowUtc.AddDays(14)),
            nowUtc);
        var session = (await tokenService.ListSessionsAsync(userId)).Single();
        var handler = new RevokeMySessionHandler(
            new RevokeMySessionCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            tokenService);

        var response = await handler.HandleAsync(new RevokeMySessionCommand(session.SessionId, "manual"));

        Assert.True(response.Revoked);
        var updated = (await tokenService.ListSessionsAsync(userId)).Single();
        Assert.NotNull(updated.RevokedAtUtc);
    }

    [Fact]
    public async Task RevokeMySessionHandler_WhenSessionMissing_ShouldThrowValidationException()
    {
        var userId = Guid.NewGuid();
        var handler = new RevokeMySessionHandler(
            new RevokeMySessionCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            new StubTokenService());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(new RevokeMySessionCommand(Guid.NewGuid(), "manual")));

        Assert.Contains(exception.Errors, error => error.Code == "session.notFound");
    }

    [Fact]
    public async Task MarkSessionSuspiciousHandler_WhenSessionExists_ShouldFlagSession()
    {
        var userId = Guid.NewGuid();
        var tokenService = new StubTokenService();
        var nowUtc = DateTime.UtcNow;
        await tokenService.RecordSessionAsync(
            userId,
            new AuthSessionTokens(
                AccessToken: "access-1",
                AccessTokenExpiresAtUtc: nowUtc.AddMinutes(15),
                RefreshToken: "refresh-1",
                RefreshTokenExpiresAtUtc: nowUtc.AddDays(14)),
            nowUtc);
        var session = (await tokenService.ListSessionsAsync(userId)).Single();
        var handler = new MarkSessionSuspiciousHandler(
            new MarkSessionSuspiciousCommandValidator(),
            new StubCurrentUserContext(isAuthenticated: true, userId: userId),
            tokenService);

        var response = await handler.HandleAsync(new MarkSessionSuspiciousCommand(session.SessionId, "unexpected_device"));

        Assert.True(response.Flagged);
        var updated = (await tokenService.ListSessionsAsync(userId)).Single();
        Assert.True(updated.IsSuspicious);
        Assert.Equal("unexpected_device", updated.SuspiciousReason);
    }

    [Fact]
    public async Task ForgotPasswordHandler_ForUnknownEmail_ShouldReturnAcceptedWithoutTokenOrEmail()
    {
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();
        var emailSender = new RecordingEmailSender();
        var handler = new ForgotPasswordHandler(
            new ForgotPasswordCommandValidator(),
            repository,
            tokenRepository,
            tokenService,
            emailSender);

        var response = await handler.HandleAsync(new ForgotPasswordCommand("unknown@example.com"));

        Assert.True(response.Accepted);
        Assert.Equal(0, tokenRepository.PasswordResetTokenCount);
        Assert.Null(emailSender.LastMessage);
    }

    [Fact]
    public async Task ForgotPasswordHandler_ForExistingEmail_ShouldPersistResetTokenAndSendEmail()
    {
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();
        var emailSender = new RecordingEmailSender();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("forgot@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddWithCredentialAsync(user, "hash:seed");
        var handler = new ForgotPasswordHandler(
            new ForgotPasswordCommandValidator(),
            repository,
            tokenRepository,
            tokenService,
            emailSender);

        var response = await handler.HandleAsync(new ForgotPasswordCommand("forgot@example.com"));

        Assert.True(response.Accepted);
        Assert.Equal(1, tokenRepository.PasswordResetTokenCount);
        Assert.NotNull(emailSender.LastMessage);
        Assert.Equal("forgot@example.com", emailSender.LastMessage!.Recipient.Value);
    }

    [Fact]
    public async Task ResetPasswordHandler_WithValidToken_ShouldUpdatePasswordAndConsumeToken()
    {
        var nowUtc = DateTime.UtcNow;
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var hasher = new StubCredentialHasher();
        var tokenService = new StubTokenService();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("reset@example.com"), UserRole.User, nowUtc);
        await repository.AddWithCredentialAsync(user, hasher.HashPassword("OldPassword123!"));

        var tokenHash = tokenService.HashOneTimeToken("reset-token");
        await tokenRepository.AddPasswordResetTokenAsync(
            PasswordResetToken.Issue(
                Guid.NewGuid(),
                user.Id,
                tokenHash,
                nowUtc.AddMinutes(-1),
                nowUtc.AddMinutes(29)));

        var handler = new ResetPasswordHandler(
            new ResetPasswordCommandValidator(),
            repository,
            tokenRepository,
            hasher,
            tokenService,
            new RecordingSecurityAuditLogger());

        var response = await handler.HandleAsync(new ResetPasswordCommand("reset-token", "NewPassword123!"));

        Assert.True(response.PasswordReset);
        var snapshot = await repository.GetCredentialSnapshotByEmailAsync(UserEmail.From("reset@example.com"));
        Assert.NotNull(snapshot);
        Assert.True(hasher.VerifyPassword("NewPassword123!", snapshot!.PasswordHash));

        var updatedToken = await tokenRepository.GetPasswordResetTokenByHashAsync(tokenHash);
        Assert.NotNull(updatedToken);
        Assert.True(updatedToken!.IsUsed);
    }

    [Fact]
    public async Task ResetPasswordHandler_WithUnknownToken_ShouldThrowValidationException()
    {
        var handler = new ResetPasswordHandler(
            new ResetPasswordCommandValidator(),
            new InMemoryUserRepository(),
            new InMemoryUserTokenRepository(),
            new StubCredentialHasher(),
            new StubTokenService(),
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new ResetPasswordCommand("missing-token", "NewPassword123!")));

        Assert.Contains(exception.Errors, error => error.Code == "token.invalid");
    }

    [Fact]
    public async Task ResetPasswordHandler_WithExpiredToken_ShouldThrowValidationException()
    {
        var nowUtc = DateTime.UtcNow;
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("expired-reset@example.com"), UserRole.User, nowUtc);
        await repository.AddWithCredentialAsync(user, "hash:seed");

        await tokenRepository.AddPasswordResetTokenAsync(
            PasswordResetToken.Issue(
                Guid.NewGuid(),
                user.Id,
                tokenService.HashOneTimeToken("reset-token"),
                nowUtc.AddHours(-2),
                nowUtc.AddHours(-1)));

        var handler = new ResetPasswordHandler(
            new ResetPasswordCommandValidator(),
            repository,
            tokenRepository,
            new StubCredentialHasher(),
            tokenService,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new ResetPasswordCommand("reset-token", "NewPassword123!")));

        Assert.Contains(exception.Errors, error => error.Code == "token.invalidOrExpired");
    }

    [Fact]
    public async Task ResetPasswordHandler_WithMissingUser_ShouldThrowValidationException()
    {
        var nowUtc = DateTime.UtcNow;
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();

        await tokenRepository.AddPasswordResetTokenAsync(
            PasswordResetToken.Issue(
                Guid.NewGuid(),
                Guid.NewGuid(),
                tokenService.HashOneTimeToken("reset-token"),
                nowUtc.AddMinutes(-1),
                nowUtc.AddMinutes(29)));

        var handler = new ResetPasswordHandler(
            new ResetPasswordCommandValidator(),
            new InMemoryUserRepository(),
            tokenRepository,
            new StubCredentialHasher(),
            tokenService,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new ResetPasswordCommand("reset-token", "NewPassword123!")));

        Assert.Contains(exception.Errors, error => error.Code == "token.invalid");
    }

    [Fact]
    public async Task VerifyEmailHandler_WithValidToken_ShouldMarkUserVerifiedAndConsumeToken()
    {
        var nowUtc = DateTime.UtcNow;
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("verify@example.com"), UserRole.User, nowUtc);
        await repository.AddWithCredentialAsync(user, "hash:seed");

        var tokenHash = tokenService.HashOneTimeToken("verify-token");
        await tokenRepository.AddEmailVerificationTokenAsync(
            EmailVerificationToken.Issue(
                Guid.NewGuid(),
                user.Id,
                tokenHash,
                nowUtc.AddMinutes(-1),
                nowUtc.AddHours(1)));

        var handler = new VerifyEmailHandler(
            new VerifyEmailCommandValidator(),
            repository,
            tokenRepository,
            tokenService,
            new RecordingSecurityAuditLogger());

        var response = await handler.HandleAsync(new VerifyEmailCommand("verify-token"));

        Assert.True(response.Verified);
        Assert.Equal("verify@example.com", response.Email);
        var persistedUser = await repository.GetByIdAsync(user.Id);
        Assert.NotNull(persistedUser);
        Assert.True(persistedUser!.IsEmailVerified);

        var updatedToken = await tokenRepository.GetEmailVerificationTokenByHashAsync(tokenHash);
        Assert.NotNull(updatedToken);
        Assert.True(updatedToken!.IsUsed);
    }

    [Fact]
    public async Task VerifyEmailHandler_WithExpiredToken_ShouldThrowValidationException()
    {
        var nowUtc = DateTime.UtcNow;
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("expired@example.com"), UserRole.User, nowUtc);
        await repository.AddWithCredentialAsync(user, "hash:seed");

        var tokenHash = tokenService.HashOneTimeToken("verify-token");
        await tokenRepository.AddEmailVerificationTokenAsync(
            EmailVerificationToken.Issue(
                Guid.NewGuid(),
                user.Id,
                tokenHash,
                nowUtc.AddHours(-2),
                nowUtc.AddHours(-1)));

        var handler = new VerifyEmailHandler(
            new VerifyEmailCommandValidator(),
            repository,
            tokenRepository,
            tokenService,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new VerifyEmailCommand("verify-token")));

        Assert.Contains(exception.Errors, error => error.Code == "token.invalidOrExpired");
    }

    [Fact]
    public async Task VerifyEmailHandler_WithMissingUser_ShouldThrowValidationException()
    {
        var nowUtc = DateTime.UtcNow;
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();

        await tokenRepository.AddEmailVerificationTokenAsync(
            EmailVerificationToken.Issue(
                Guid.NewGuid(),
                Guid.NewGuid(),
                tokenService.HashOneTimeToken("verify-token"),
                nowUtc.AddMinutes(-1),
                nowUtc.AddHours(1)));

        var handler = new VerifyEmailHandler(
            new VerifyEmailCommandValidator(),
            new InMemoryUserRepository(),
            tokenRepository,
            tokenService,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new VerifyEmailCommand("verify-token")));

        Assert.Contains(exception.Errors, error => error.Code == "token.invalid");
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenUserAlreadyVerified_ShouldThrowValidationException()
    {
        var nowUtc = DateTime.UtcNow;
        var repository = new InMemoryUserRepository();
        var tokenRepository = new InMemoryUserTokenRepository();
        var tokenService = new StubTokenService();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("already-verified@example.com"), UserRole.User, nowUtc);
        user.MarkEmailVerified(nowUtc.AddMinutes(1));
        await repository.AddWithCredentialAsync(user, "hash:seed");

        await tokenRepository.AddEmailVerificationTokenAsync(
            EmailVerificationToken.Issue(
                Guid.NewGuid(),
                user.Id,
                tokenService.HashOneTimeToken("verify-token"),
                nowUtc.AddMinutes(-1),
                nowUtc.AddHours(1)));

        var handler = new VerifyEmailHandler(
            new VerifyEmailCommandValidator(),
            repository,
            tokenRepository,
            tokenService,
            new RecordingSecurityAuditLogger());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new VerifyEmailCommand("verify-token")));

        Assert.Contains(exception.Errors, error => error.Code == "email.alreadyVerified");
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

    private sealed class InMemoryUserTokenRepository : IUserTokenRepository
    {
        private readonly Dictionary<string, EmailVerificationToken> _emailVerificationTokensByHash = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PasswordResetToken> _passwordResetTokensByHash = new(StringComparer.Ordinal);

        public int EmailVerificationTokenCount => _emailVerificationTokensByHash.Count;

        public int PasswordResetTokenCount => _passwordResetTokensByHash.Count;

        public Task AddEmailVerificationTokenAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken = default)
        {
            _emailVerificationTokensByHash[token.TokenHash.Value] = token;
            return Task.CompletedTask;
        }

        public Task<EmailVerificationToken?> GetEmailVerificationTokenByHashAsync(
            TokenHash tokenHash,
            CancellationToken cancellationToken = default)
        {
            _emailVerificationTokensByHash.TryGetValue(tokenHash.Value, out var token);
            return Task.FromResult(token);
        }

        public Task UpdateEmailVerificationTokenAsync(
            EmailVerificationToken token,
            CancellationToken cancellationToken = default)
        {
            _emailVerificationTokensByHash[token.TokenHash.Value] = token;
            return Task.CompletedTask;
        }

        public Task AddPasswordResetTokenAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default)
        {
            _passwordResetTokensByHash[token.TokenHash.Value] = token;
            return Task.CompletedTask;
        }

        public Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(
            TokenHash tokenHash,
            CancellationToken cancellationToken = default)
        {
            _passwordResetTokensByHash.TryGetValue(tokenHash.Value, out var token);
            return Task.FromResult(token);
        }

        public Task UpdatePasswordResetTokenAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default)
        {
            _passwordResetTokensByHash[token.TokenHash.Value] = token;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCredentialHasher : ICredentialHasher
    {
        public string HashPassword(string plainPassword) => $"hash:{plainPassword}";

        public bool VerifyPassword(string plainPassword, string passwordHash)
        {
            return passwordHash == HashPassword(plainPassword);
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

    private sealed class RecordingSecurityAuditLogger : ISecurityAuditLogger
    {
        public int LoginFailureCount { get; private set; }

        public int PasswordResetCount { get; private set; }

        public int EmailVerifiedCount { get; private set; }

        public int UserRoleChangedCount { get; private set; }

        public Task LoginFailedAsync(
            string email,
            DateTime occurredAtUtc,
            bool accountLocked,
            DateTime? lockedUntilUtc,
            CancellationToken cancellationToken = default)
        {
            LoginFailureCount++;
            return Task.CompletedTask;
        }

        public Task PasswordResetAsync(
            Guid userId,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            PasswordResetCount++;
            return Task.CompletedTask;
        }

        public Task EmailVerifiedAsync(
            Guid userId,
            string email,
            DateTime occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            EmailVerifiedCount++;
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

    private sealed class StubTokenService : IAuthTokenService
    {
        private readonly Dictionary<Guid, AuthSessionRecord> _sessionsById = new();

        public int SessionIssueCount { get; private set; }

        public int RecordedSessionCount { get; private set; }

        public int EmailVerificationIssueCount { get; private set; }

        public int PasswordResetIssueCount { get; private set; }

        public Guid? LastRevokedUserId { get; private set; }

        public string? LastRevokedRefreshToken { get; private set; }

        public AuthSessionTokens IssueSessionTokens(User user, DateTime issuedAtUtc)
        {
            SessionIssueCount++;
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
            RecordedSessionCount++;
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
            EmailVerificationIssueCount++;
            return new OneTimeTokenIssue(
                PlainTextToken: "verify-token",
                TokenHash: TokenHash.From(new string('a', 64)),
                ExpiresAtUtc: issuedAtUtc.AddHours(24));
        }

        public OneTimeTokenIssue IssuePasswordResetToken(DateTime issuedAtUtc)
        {
            PasswordResetIssueCount++;
            return new OneTimeTokenIssue(
                PlainTextToken: "reset-token",
                TokenHash: TokenHash.From(new string('b', 64)),
                ExpiresAtUtc: issuedAtUtc.AddMinutes(30));
        }

        public TokenHash HashOneTimeToken(string plainToken)
        {
            if (string.Equals(plainToken, "verify-token", StringComparison.Ordinal))
            {
                return TokenHash.From(new string('a', 64));
            }

            if (string.Equals(plainToken, "reset-token", StringComparison.Ordinal))
            {
                return TokenHash.From(new string('b', 64));
            }

            return TokenHash.From(new string('c', 64));
        }

        public Task RevokeSessionAsync(
            Guid userId,
            string? refreshToken,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastRevokedUserId = userId;
            LastRevokedRefreshToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AuthSessionRecord>> ListSessionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var records = _sessionsById.Values.Where(session => session.UserId == userId).ToArray();
            return Task.FromResult<IReadOnlyCollection<AuthSessionRecord>>(records);
        }

        public Task<bool> RevokeSessionByIdAsync(
            Guid userId,
            Guid sessionId,
            string? reason,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default)
        {
            if (!_sessionsById.TryGetValue(sessionId, out var session) || session.UserId != userId)
            {
                return Task.FromResult(false);
            }

            _sessionsById[sessionId] = session with
            {
                RevokedAtUtc = revokedAtUtc,
                RevocationReason = reason
            };
            return Task.FromResult(true);
        }

        public Task<bool> MarkSessionSuspiciousAsync(
            Guid userId,
            Guid sessionId,
            string reason,
            DateTime markedAtUtc,
            CancellationToken cancellationToken = default)
        {
            if (!_sessionsById.TryGetValue(sessionId, out var session) || session.UserId != userId)
            {
                return Task.FromResult(false);
            }

            _sessionsById[sessionId] = session with
            {
                IsSuspicious = true,
                SuspiciousMarkedAtUtc = markedAtUtc,
                SuspiciousReason = reason
            };
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryLoginAttemptLockoutService : ILoginAttemptLockoutService
    {
        private readonly Dictionary<string, int> _failedAttemptsByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _lockedUntilByEmail = new(StringComparer.OrdinalIgnoreCase);

        public Task<LoginAttemptLockoutState> GetStateAsync(string email, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            if (_lockedUntilByEmail.TryGetValue(email, out var lockedUntilUtc) && lockedUntilUtc > nowUtc)
            {
                return Task.FromResult(new LoginAttemptLockoutState(IsLockedOut: true, LockedUntilUtc: lockedUntilUtc));
            }

            _lockedUntilByEmail.Remove(email);
            return Task.FromResult(new LoginAttemptLockoutState(IsLockedOut: false, LockedUntilUtc: null));
        }

        public Task<LoginAttemptLockoutState> RegisterFailedAttemptAsync(string email, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var current = _failedAttemptsByEmail.TryGetValue(email, out var attempts) ? attempts : 0;
            current++;
            _failedAttemptsByEmail[email] = current;

            if (current >= 5)
            {
                var lockedUntilUtc = nowUtc.AddMinutes(15);
                _lockedUntilByEmail[email] = lockedUntilUtc;
                return Task.FromResult(new LoginAttemptLockoutState(IsLockedOut: true, LockedUntilUtc: lockedUntilUtc));
            }

            return Task.FromResult(new LoginAttemptLockoutState(IsLockedOut: false, LockedUntilUtc: null));
        }

        public Task ResetAsync(string email, CancellationToken cancellationToken = default)
        {
            _failedAttemptsByEmail.Remove(email);
            _lockedUntilByEmail.Remove(email);
            return Task.CompletedTask;
        }
    }
}
