using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record LogoutCommand(string? RefreshToken) : IRequest<LogoutResponse>;

