using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Infrastructure.Auth;

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(AuthEmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
