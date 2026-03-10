namespace Anizaki.Application.Features.Auth.Contracts;

public interface IEmailSender
{
    Task SendAsync(AuthEmailMessage message, CancellationToken cancellationToken = default);
}

