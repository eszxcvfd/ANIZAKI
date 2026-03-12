using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

public sealed class GetDrawingPreviewHandler : IRequestHandler<GetDrawingPreviewQuery, GetDrawingPreviewResponse>
{
    private readonly ILibraryRepository _repository;
    private readonly IRequestValidator<GetDrawingPreviewQuery> _validator;

    public GetDrawingPreviewHandler(
        ILibraryRepository repository,
        IRequestValidator<GetDrawingPreviewQuery> validator)
    {
        _repository = repository;
        _validator  = validator;
    }

    public async Task<GetDrawingPreviewResponse> HandleAsync(
        GetDrawingPreviewQuery request,
        CancellationToken cancellationToken = default)
    {
        var result = _validator.Validate(request);
        if (!result.IsValid)
            throw new RequestValidationException(result.Errors);

        var preview = await _repository.GetDrawingPreviewAsync(request.DrawingId, cancellationToken);
        if (preview is null)
            throw new ResourceNotFoundException("Drawing", request.DrawingId.ToString());

        return preview;
    }
}
