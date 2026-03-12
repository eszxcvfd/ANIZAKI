using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

/// <summary>
/// GetCategoriesQuery has no parameters — always valid.
/// </summary>
public sealed class GetCategoriesQueryValidator : IRequestValidator<GetCategoriesQuery>
{
    public ValidationResult Validate(GetCategoriesQuery request) => ValidationResult.Success;
}
