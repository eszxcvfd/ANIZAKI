using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class GetMySessionsQueryValidator : IRequestValidator<GetMySessionsQuery>
{
    public ValidationResult Validate(GetMySessionsQuery request)
    {
        return ValidationResult.Success;
    }
}

