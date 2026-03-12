using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

public sealed class GetDrawingDetailHandler : IRequestHandler<GetDrawingDetailQuery, GetDrawingDetailResponse>
{
    private readonly ILibraryRepository _repository;
    private readonly IRequestValidator<GetDrawingDetailQuery> _validator;

    public GetDrawingDetailHandler(
        ILibraryRepository repository,
        IRequestValidator<GetDrawingDetailQuery> validator)
    {
        _repository = repository;
        _validator  = validator;
    }

    public async Task<GetDrawingDetailResponse> HandleAsync(
        GetDrawingDetailQuery request,
        CancellationToken cancellationToken = default)
    {
        var result = _validator.Validate(request);
        if (!result.IsValid)
            throw new RequestValidationException(result.Errors);

        var drawing = await _repository.GetDrawingDetailAsync(request.DrawingId, cancellationToken);
        if (drawing is null)
            throw new ResourceNotFoundException("Drawing", request.DrawingId.ToString());

        return drawing;
    }
}
