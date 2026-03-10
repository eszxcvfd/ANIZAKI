using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record UserCredentialSnapshot(
    User User,
    string PasswordHash);

