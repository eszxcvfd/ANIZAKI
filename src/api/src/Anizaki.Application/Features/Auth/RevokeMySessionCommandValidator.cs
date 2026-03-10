using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class RevokeMySessionCommandValidator : IRequestValidator<RevokeMySessionCommand>
{
    public ValidationResult Validate(RevokeMySessionCommand request)
    {
        var errors = new List<ValidationError>();

        if (request.SessionId == Guid.Empty)
        {
            errors.Add(new ValidationError("sessionId", "session.required", "Session id is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Length > 200)
        {
            errors.Add(new ValidationError("reason", "session.reasonTooLong", "Reason cannot exceed 200 characters."));
        }

        return ValidationResult.FromErrors(errors);
    }
}

