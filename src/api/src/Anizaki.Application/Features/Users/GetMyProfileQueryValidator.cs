using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Users.Contracts;

namespace Anizaki.Application.Features.Users;

public sealed class GetMyProfileQueryValidator : IRequestValidator<GetMyProfileQuery>
{
    public ValidationResult Validate(GetMyProfileQuery request)
    {
        return ValidationResult.Success;
    }
}

