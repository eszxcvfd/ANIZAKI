using System;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

public sealed class GetDrawingDetailQueryValidator : IRequestValidator<GetDrawingDetailQuery>
{
    public ValidationResult Validate(GetDrawingDetailQuery request)
    {
        if (request.DrawingId == Guid.Empty)
        {
            return ValidationResult.FromErrors([
                new ValidationError("drawingId", "drawingId.empty", "Drawing ID must not be empty.")
            ]);
        }

        return ValidationResult.Success;
    }
}
