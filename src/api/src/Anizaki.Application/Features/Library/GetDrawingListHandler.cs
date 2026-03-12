using System;
using System.Linq;
using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

public sealed class GetDrawingListHandler : IRequestHandler<GetDrawingListQuery, GetDrawingListResponse>
{
    private readonly ILibraryRepository _repository;
    private readonly IRequestValidator<GetDrawingListQuery> _validator;

    public GetDrawingListHandler(
        ILibraryRepository repository,
        IRequestValidator<GetDrawingListQuery> validator)
    {
        _repository = repository;
        _validator  = validator;
    }

    public async Task<GetDrawingListResponse> HandleAsync(
        GetDrawingListQuery request,
        CancellationToken cancellationToken = default)
    {
        var result = _validator.Validate(request);
        if (!result.IsValid)
            throw new RequestValidationException(result.Errors);

        var (items, totalItems) = await _repository.GetDrawingsAsync(
            request.Page,
            request.PageSize,
            request.Category,
            request.Search,
            request.SortBy,
            request.SortDir,
            cancellationToken);

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling((double)totalItems / request.PageSize);

        return new GetDrawingListResponse(
            Items: items,
            Pagination: new PaginationMetadata(
                Page:       request.Page,
                PageSize:   request.PageSize,
                TotalItems: totalItems,
                TotalPages: totalPages));
    }
}
