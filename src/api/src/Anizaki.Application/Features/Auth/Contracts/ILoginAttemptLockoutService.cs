namespace Anizaki.Application.Features.Auth.Contracts;

public interface ILoginAttemptLockoutService
{
    Task<LoginAttemptLockoutState> GetStateAsync(
        string email,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<LoginAttemptLockoutState> RegisterFailedAttemptAsync(
        string email,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task ResetAsync(
        string email,
        CancellationToken cancellationToken = default);
}

public sealed record LoginAttemptLockoutState(
    bool IsLockedOut,
    DateTime? LockedUntilUtc);
