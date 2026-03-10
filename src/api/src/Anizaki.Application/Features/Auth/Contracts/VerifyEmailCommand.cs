using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResponse>;

