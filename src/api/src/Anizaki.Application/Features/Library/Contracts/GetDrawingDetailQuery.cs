using System;
using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetDrawingDetailQuery(Guid DrawingId) : IRequest<GetDrawingDetailResponse>;
