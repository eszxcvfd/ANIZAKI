using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

