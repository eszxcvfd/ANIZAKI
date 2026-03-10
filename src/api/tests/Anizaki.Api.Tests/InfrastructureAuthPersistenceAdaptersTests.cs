using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;
using Anizaki.Infrastructure.Auth;

namespace Anizaki.Api.Tests;

public class InfrastructureAuthPersistenceAdaptersTests
{
    [Fact]
    public async Task InMemoryUserRepository_WithDuplicateEmailAcrossDifferentUsers_ShouldThrow()
    {
        var repository = new InMemoryUserRepository();
        var firstUser = User.Create(Guid.NewGuid(), UserEmail.From("dup@example.com"), UserRole.User, DateTime.UtcNow);
        var secondUser = User.Create(Guid.NewGuid(), UserEmail.From("dup@example.com"), UserRole.Seller, DateTime.UtcNow);

        await repository.AddAsync(firstUser);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(secondUser));
    }

    [Fact]
    public async Task InMemoryUserRepository_ShouldRoundTripCredentialSnapshot()
    {
        var repository = new InMemoryUserRepository();
        var user = User.Create(Guid.NewGuid(), UserEmail.From("snapshot@example.com"), UserRole.User, DateTime.UtcNow);
        await repository.AddWithCredentialAsync(user, "hash:value");

        var snapshot = await repository.GetCredentialSnapshotByEmailAsync(UserEmail.From("snapshot@example.com"));

        Assert.NotNull(snapshot);
        Assert.Equal(user.Id, snapshot!.User.Id);
        Assert.Equal("hash:value", snapshot.PasswordHash);
    }

    [Fact]
    public async Task InMemoryUserTokenRepository_ShouldStoreAndUpdateTokensByHash()
    {
        var repository = new InMemoryUserTokenRepository();
        var userId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow.AddMinutes(-1);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(29);
        var verifyHash = TokenHash.From(new string('a', 64));
        var resetHash = TokenHash.From(new string('b', 64));
        var verifyToken = EmailVerificationToken.Issue(Guid.NewGuid(), userId, verifyHash, createdAtUtc, expiresAtUtc);
        var resetToken = PasswordResetToken.Issue(Guid.NewGuid(), userId, resetHash, createdAtUtc, expiresAtUtc);

        await repository.AddEmailVerificationTokenAsync(verifyToken);
        await repository.AddPasswordResetTokenAsync(resetToken);

        var loadedVerifyToken = await repository.GetEmailVerificationTokenByHashAsync(verifyHash);
        var loadedResetToken = await repository.GetPasswordResetTokenByHashAsync(resetHash);
        Assert.NotNull(loadedVerifyToken);
        Assert.NotNull(loadedResetToken);
        Assert.False(loadedVerifyToken!.IsUsed);
        Assert.False(loadedResetToken!.IsUsed);

        var usedAtUtc = DateTime.UtcNow;
        await repository.UpdateEmailVerificationTokenAsync(loadedVerifyToken.MarkUsed(usedAtUtc));
        await repository.UpdatePasswordResetTokenAsync(loadedResetToken.MarkUsed(usedAtUtc));

        var updatedVerifyToken = await repository.GetEmailVerificationTokenByHashAsync(verifyHash);
        var updatedResetToken = await repository.GetPasswordResetTokenByHashAsync(resetHash);
        Assert.True(updatedVerifyToken!.IsUsed);
        Assert.True(updatedResetToken!.IsUsed);
    }

    [Fact]
    public async Task InMemoryLoginAttemptLockoutService_ShouldLockAfterThresholdFailures()
    {
        var service = new InMemoryLoginAttemptLockoutService();
        var email = "lockout@example.com";
        var nowUtc = DateTime.UtcNow;

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var state = await service.RegisterFailedAttemptAsync(email, nowUtc);
            Assert.False(state.IsLockedOut);
        }

        var lockedState = await service.RegisterFailedAttemptAsync(email, nowUtc);

        Assert.True(lockedState.IsLockedOut);
        Assert.NotNull(lockedState.LockedUntilUtc);
    }

    [Fact]
    public async Task InMemoryLoginAttemptLockoutService_Reset_ShouldClearLockoutState()
    {
        var service = new InMemoryLoginAttemptLockoutService();
        var email = "reset-lockout@example.com";
        var nowUtc = DateTime.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await service.RegisterFailedAttemptAsync(email, nowUtc);
        }

        await service.ResetAsync(email);
        var state = await service.GetStateAsync(email, nowUtc);

        Assert.False(state.IsLockedOut);
        Assert.Null(state.LockedUntilUtc);
    }

    [Fact]
    public async Task InMemoryAuthSessionRepository_ShouldRoundTripSessionStateByIdAndHash()
    {
        var repository = new InMemoryAuthSessionRepository();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var refreshHash = TokenHash.From(new string('c', 64));
        var session = new AuthSessionRecord(
            SessionId: sessionId,
            UserId: userId,
            RefreshTokenHash: refreshHash,
            IssuedAtUtc: DateTime.UtcNow.AddMinutes(-2),
            AccessTokenExpiresAtUtc: DateTime.UtcNow.AddMinutes(13),
            RefreshTokenExpiresAtUtc: DateTime.UtcNow.AddDays(14),
            RevokedAtUtc: null,
            RevocationReason: null,
            IsSuspicious: false,
            SuspiciousMarkedAtUtc: null,
            SuspiciousReason: null);

        await repository.AddOrUpdateAsync(session);

        var byId = await repository.GetBySessionIdAsync(userId, sessionId);
        var byHash = await repository.GetByRefreshTokenHashAsync(userId, refreshHash);
        var listed = await repository.ListByUserAsync(userId);

        Assert.NotNull(byId);
        Assert.NotNull(byHash);
        Assert.Single(listed);
    }
}
