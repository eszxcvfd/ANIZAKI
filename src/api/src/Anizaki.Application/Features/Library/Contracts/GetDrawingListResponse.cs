using System;
using System.Collections.Generic;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetDrawingListResponse(
    IReadOnlyCollection<DrawingItemDto> Items,
    PaginationMetadata Pagination);

public sealed record DrawingItemDto(
    Guid Id,
    string Title,
    string Code,
    string CategorySlug,
    string CategoryName,
    string Status,
    DateTime CreatedAtUtc,
    string? PreviewUrl);

public sealed record PaginationMetadata(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
