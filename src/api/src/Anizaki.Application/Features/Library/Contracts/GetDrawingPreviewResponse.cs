using System;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetDrawingPreviewResponse(
    Guid DrawingId,
    string PreviewAvailability,
    string? PreviewType,
    string? PreviewUrl,
    string? Message);
