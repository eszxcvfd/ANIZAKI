using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record RevokeMySessionCommand(Guid SessionId, string? Reason) : IRequest<RevokeMySessionResponse>;

