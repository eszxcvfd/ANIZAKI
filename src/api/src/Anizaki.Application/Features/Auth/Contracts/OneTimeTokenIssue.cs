using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record OneTimeTokenIssue(
    string PlainTextToken,
    TokenHash TokenHash,
    DateTime ExpiresAtUtc);

