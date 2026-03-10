using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class LoginCommandValidator : IRequestValidator<LoginCommand>
{
    public ValidationResult Validate(LoginCommand request)
    {
        List<ValidationError> errors = [];

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError("email", "email.empty", "Email is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(new ValidationError("password", "password.empty", "Password is required."));
        }

        return ValidationResult.FromErrors(errors);
    }
}

