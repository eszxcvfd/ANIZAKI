using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class ForgotPasswordCommandValidator : IRequestValidator<ForgotPasswordCommand>
{
    public ValidationResult Validate(ForgotPasswordCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return ValidationResult.FromErrors([
                new ValidationError("email", "email.empty", "Email is required.")
            ]);
        }

        return ValidationResult.Success;
    }
}

