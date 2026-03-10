using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record RegisterUserCommand(string Email, string Password) : IRequest<RegisterUserResponse>;

