using System;
using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetDrawingPreviewQuery(Guid DrawingId) : IRequest<GetDrawingPreviewResponse>;
