using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class LogoutCommandValidator : IRequestValidator<LogoutCommand>
{
    public ValidationResult Validate(LogoutCommand request)
    {
        if (request.RefreshToken is null)
        {
            return ValidationResult.Success;
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ValidationResult.FromErrors([
                new ValidationError("refreshToken", "refreshToken.empty", "Refresh token cannot be blank when provided.")
            ]);
        }

        return ValidationResult.Success;
    }
}

