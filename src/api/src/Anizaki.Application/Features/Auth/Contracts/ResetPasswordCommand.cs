using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ResetPasswordResponse>;

