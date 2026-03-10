using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Infrastructure.Auth;

public sealed class LoggerSecurityAuditLogger : ISecurityAuditLogger
{
    public LoggerSecurityAuditLogger()
    {
    }

    public Task LoginFailedAsync(
        string email,
        DateTime occurredAtUtc,
        bool accountLocked,
        DateTime? lockedUntilUtc,
        CancellationToken cancellationToken = default)
    {
        WriteAuditLine(
            "login_failed",
            $"email={email}",
            $"accountLocked={accountLocked}",
            $"lockedUntilUtc={lockedUntilUtc:O}",
            $"occurredAtUtc={occurredAtUtc:O}");

        return Task.CompletedTask;
    }

    public Task PasswordResetAsync(
        Guid userId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        WriteAuditLine(
            "password_reset",
            $"userId={userId}",
            $"occurredAtUtc={occurredAtUtc:O}");

        return Task.CompletedTask;
    }

    public Task EmailVerifiedAsync(
        Guid userId,
        string email,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        WriteAuditLine(
            "email_verified",
            $"userId={userId}",
            $"email={email}",
            $"occurredAtUtc={occurredAtUtc:O}");

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
        WriteAuditLine(
            "user_role_changed",
            $"actorUserId={actorUserId}",
            $"targetUserId={targetUserId}",
            $"previousRole={previousRole}",
            $"nextRole={nextRole}",
            $"occurredAtUtc={occurredAtUtc:O}");

        return Task.CompletedTask;
    }

    private static void WriteAuditLine(string eventName, params string[] fields)
    {
        Console.WriteLine($"security_audit event={eventName} {string.Join(' ', fields)}");
    }
}
