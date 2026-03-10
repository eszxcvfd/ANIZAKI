namespace Anizaki.Application.Features.Auth.Contracts;

public interface ISecurityAuditLogger
{
    Task LoginFailedAsync(
        string email,
        DateTime occurredAtUtc,
        bool accountLocked,
        DateTime? lockedUntilUtc,
        CancellationToken cancellationToken = default);

    Task PasswordResetAsync(
        Guid userId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task EmailVerifiedAsync(
        Guid userId,
        string email,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    Task UserRoleChangedAsync(
        Guid actorUserId,
        Guid targetUserId,
        string previousRole,
        string nextRole,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);
}
