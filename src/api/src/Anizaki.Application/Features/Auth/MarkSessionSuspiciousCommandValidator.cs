using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class MarkSessionSuspiciousCommandValidator : IRequestValidator<MarkSessionSuspiciousCommand>
{
    public ValidationResult Validate(MarkSessionSuspiciousCommand request)
    {
        var errors = new List<ValidationError>();
        if (request.SessionId == Guid.Empty)
        {
            errors.Add(new ValidationError("sessionId", "session.required", "Session id is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            errors.Add(new ValidationError("reason", "session.reasonRequired", "Reason is required."));
        }
        else if (request.Reason.Length > 200)
        {
            errors.Add(new ValidationError("reason", "session.reasonTooLong", "Reason cannot exceed 200 characters."));
        }

        return ValidationResult.FromErrors(errors);
    }
}

