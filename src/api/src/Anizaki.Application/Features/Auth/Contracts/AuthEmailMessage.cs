using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record AuthEmailMessage(
    UserEmail Recipient,
    string Subject,
    string TextBody,
    string? HtmlBody);

