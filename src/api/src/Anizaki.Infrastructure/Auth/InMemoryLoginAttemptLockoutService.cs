using System.Collections.Concurrent;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Infrastructure.Auth;

public sealed class InMemoryLoginAttemptLockoutService : ILoginAttemptLockoutService
{
    private const int FailureThreshold = 5;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, AttemptState> _attemptsByEmail = new();

    public Task<LoginAttemptLockoutState> GetStateAsync(
        string email,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Normalize(email);
        if (!_attemptsByEmail.TryGetValue(normalizedEmail, out var state))
        {
            return Task.FromResult(new LoginAttemptLockoutState(IsLockedOut: false, LockedUntilUtc: null));
        }

        lock (state.Gate)
        {
            PruneExpiredAttempts(state, nowUtc);
            return Task.FromResult(ToLockoutState(state, nowUtc));
        }
    }

    public Task<LoginAttemptLockoutState> RegisterFailedAttemptAsync(
        string email,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Normalize(email);
        var state = _attemptsByEmail.GetOrAdd(normalizedEmail, _ => new AttemptState());

        lock (state.Gate)
        {
            PruneExpiredAttempts(state, nowUtc);
            var currentState = ToLockoutState(state, nowUtc);
            if (currentState.IsLockedOut)
            {
                return Task.FromResult(currentState);
            }

            state.FailedAttemptTimestampsUtc.Enqueue(nowUtc);
            if (state.FailedAttemptTimestampsUtc.Count >= FailureThreshold)
            {
                state.LockedUntilUtc = nowUtc.Add(LockoutDuration);
                state.FailedAttemptTimestampsUtc.Clear();
            }

            return Task.FromResult(ToLockoutState(state, nowUtc));
        }
    }

    public Task ResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Normalize(email);
        _attemptsByEmail.TryRemove(normalizedEmail, out _);
        return Task.CompletedTask;
    }

    private static void PruneExpiredAttempts(AttemptState state, DateTime nowUtc)
    {
        while (state.FailedAttemptTimestampsUtc.TryPeek(out var timestamp) &&
               nowUtc - timestamp > FailureWindow)
        {
            state.FailedAttemptTimestampsUtc.Dequeue();
        }

        if (state.LockedUntilUtc is not null && state.LockedUntilUtc <= nowUtc)
        {
            state.LockedUntilUtc = null;
        }
    }

    private static LoginAttemptLockoutState ToLockoutState(AttemptState state, DateTime nowUtc)
    {
        var isLockedOut = state.LockedUntilUtc is not null && state.LockedUntilUtc > nowUtc;
        return new LoginAttemptLockoutState(
            IsLockedOut: isLockedOut,
            LockedUntilUtc: isLockedOut ? state.LockedUntilUtc : null);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private sealed class AttemptState
    {
        public object Gate { get; } = new();

        public Queue<DateTime> FailedAttemptTimestampsUtc { get; } = new();

        public DateTime? LockedUntilUtc { get; set; }
    }
}
