using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record MarkSessionSuspiciousCommand(Guid SessionId, string Reason) : IRequest<MarkSessionSuspiciousResponse>;

