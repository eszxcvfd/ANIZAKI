using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetDrawingListQuery(
    int Page,
    int PageSize,
    string? Category,
    string? Search,
    string? SortBy  = null,
    string? SortDir = null) : IRequest<GetDrawingListResponse>;
