using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class ResetPasswordCommandValidator : IRequestValidator<ResetPasswordCommand>
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    public ValidationResult Validate(ResetPasswordCommand request)
    {
        List<ValidationError> errors = [];

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            errors.Add(new ValidationError("token", "token.empty", "Reset token is required."));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors.Add(new ValidationError("newPassword", "newPassword.empty", "New password is required."));
        }
        else
        {
            if (request.NewPassword.Length < MinPasswordLength)
            {
                errors.Add(new ValidationError("newPassword", "newPassword.tooShort", $"Password must be at least {MinPasswordLength} characters."));
            }

            if (request.NewPassword.Length > MaxPasswordLength)
            {
                errors.Add(new ValidationError("newPassword", "newPassword.tooLong", $"Password must not exceed {MaxPasswordLength} characters."));
            }
        }

        return ValidationResult.FromErrors(errors);
    }
}

