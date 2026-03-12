using System.Linq;
using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

public sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, GetCategoriesResponse>
{
    private readonly ILibraryRepository _repository;
    private readonly IRequestValidator<GetCategoriesQuery> _validator;

    public GetCategoriesHandler(
        ILibraryRepository repository,
        IRequestValidator<GetCategoriesQuery> validator)
    {
        _repository = repository;
        _validator  = validator;
    }

    public async Task<GetCategoriesResponse> HandleAsync(
        GetCategoriesQuery request,
        CancellationToken cancellationToken = default)
    {
        var result = _validator.Validate(request);
        if (!result.IsValid)
            throw new RequestValidationException(result.Errors);

        var categories = await _repository.GetCategoriesAsync(cancellationToken);

        return new GetCategoriesResponse(
            Items: categories
                .OrderBy(c => c.Order)
                .ToList());
    }
}
