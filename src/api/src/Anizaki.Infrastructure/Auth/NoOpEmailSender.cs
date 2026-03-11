using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Infrastructure.Auth;

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(AuthEmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"\n[NO-OP EMAIL SENDER]\nEMAIL SENT TO: {message.Recipient.Value}\nSUBJECT: {message.Subject}\nBODY: {message.TextBody}\nHTML: {message.HtmlBody}\n");
        return Task.CompletedTask;
    }
}
