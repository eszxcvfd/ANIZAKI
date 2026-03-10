using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Users.Contracts;

namespace Anizaki.Application.Features.Users;

public sealed class UpdateMyProfileCommandValidator : IRequestValidator<UpdateMyProfileCommand>
{
    public ValidationResult Validate(UpdateMyProfileCommand request)
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

