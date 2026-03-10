using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class RegisterUserCommandValidator : IRequestValidator<RegisterUserCommand>
{
    public const int MaxEmailLength = 320;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public ValidationResult Validate(RegisterUserCommand request)
    {
        List<ValidationError> errors = [];

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError("email", "email.empty", "Email is required."));
        }
        else if (request.Email.Trim().Length > MaxEmailLength)
        {
            errors.Add(new ValidationError("email", "email.tooLong", $"Email must not exceed {MaxEmailLength} characters."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(new ValidationError("password", "password.empty", "Password is required."));
        }
        else
        {
            var length = request.Password.Length;
            if (length < MinPasswordLength)
            {
                errors.Add(new ValidationError("password", "password.tooShort", $"Password must be at least {MinPasswordLength} characters."));
            }

            if (length > MaxPasswordLength)
            {
                errors.Add(new ValidationError("password", "password.tooLong", $"Password must not exceed {MaxPasswordLength} characters."));
            }
        }

        return ValidationResult.FromErrors(errors);
    }
}

