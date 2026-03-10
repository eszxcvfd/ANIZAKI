using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public interface IUserTokenRepository
{
    Task AddEmailVerificationTokenAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> GetEmailVerificationTokenByHashAsync(
        TokenHash tokenHash,
        CancellationToken cancellationToken = default);

    Task UpdateEmailVerificationTokenAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default);

    Task AddPasswordResetTokenAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(
        TokenHash tokenHash,
        CancellationToken cancellationToken = default);

    Task UpdatePasswordResetTokenAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);
}

