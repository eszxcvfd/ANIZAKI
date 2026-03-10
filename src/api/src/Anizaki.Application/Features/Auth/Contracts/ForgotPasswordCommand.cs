using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponse>;

