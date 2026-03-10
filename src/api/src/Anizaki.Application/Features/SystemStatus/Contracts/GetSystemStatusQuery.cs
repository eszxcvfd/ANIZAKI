using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.SystemStatus.Contracts;

public sealed record GetSystemStatusQuery(string? CorrelationId = null) : IRequest<SystemStatusResponse>;
