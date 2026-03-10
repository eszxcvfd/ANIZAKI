using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Users.Contracts;

namespace Anizaki.Application.Features.Users;

public sealed class UpdateUserRoleCommandValidator : IRequestValidator<UpdateUserRoleCommand>
{
    public ValidationResult Validate(UpdateUserRoleCommand request)
    {
        List<ValidationError> errors = [];

        if (request.UserId == Guid.Empty)
        {
            errors.Add(new ValidationError("userId", "userId.empty", "Target user id is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            errors.Add(new ValidationError("role", "role.empty", "Role is required."));
        }

        return ValidationResult.FromErrors(errors);
    }
}
