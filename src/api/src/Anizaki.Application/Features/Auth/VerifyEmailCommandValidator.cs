using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class VerifyEmailCommandValidator : IRequestValidator<VerifyEmailCommand>
{
    public ValidationResult Validate(VerifyEmailCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ValidationResult.FromErrors([
                new ValidationError("token", "token.empty", "Verification token is required.")
            ]);
        }

        return ValidationResult.Success;
    }
}

