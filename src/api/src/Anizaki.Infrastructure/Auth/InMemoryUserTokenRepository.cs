using System.Collections.Concurrent;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Users;

namespace Anizaki.Infrastructure.Auth;

public sealed class InMemoryUserTokenRepository : IUserTokenRepository
{
    private readonly ConcurrentDictionary<string, EmailVerificationToken> _emailVerificationTokensByHash = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PasswordResetToken> _passwordResetTokensByHash = new(StringComparer.Ordinal);

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

