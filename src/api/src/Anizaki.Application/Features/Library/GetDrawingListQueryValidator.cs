using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Application.Features.Library;

public sealed class GetDrawingListQueryValidator : IRequestValidator<GetDrawingListQuery>
{
    private const int MinPage     = 1;
    private const int MaxPageSize = 100;
    private const int MaxSearchLength   = 200;
    private const int MaxCategoryLength = 100;

    private static readonly IReadOnlySet<string> AllowedSortBy =
        new HashSet<string>(StringComparer.Ordinal) { "createdAt", "title", "code" };

    private static readonly IReadOnlySet<string> AllowedSortDir =
        new HashSet<string>(StringComparer.Ordinal) { "asc", "desc" };

    public ValidationResult Validate(GetDrawingListQuery request)
    {
        List<ValidationError> errors = [];

        if (request.Page < MinPage)
        {
            errors.Add(new ValidationError("page", "page.invalid",
                $"Page must be at least {MinPage}."));
        }

        if (request.PageSize < 1 || request.PageSize > MaxPageSize)
        {
            errors.Add(new ValidationError("pageSize", "pageSize.invalid",
                $"PageSize must be between 1 and {MaxPageSize}."));
        }

        if (request.Category is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                errors.Add(new ValidationError("category", "category.empty",
                    "Category filter must not be blank if provided."));
            }
            else if (request.Category.Length > MaxCategoryLength)
            {
                errors.Add(new ValidationError("category", "category.tooLong",
                    $"Category must not exceed {MaxCategoryLength} characters."));
            }
        }

        if (request.Search is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Search))
            {
                errors.Add(new ValidationError("search", "search.empty",
                    "Search term must not be blank if provided."));
            }
            else if (request.Search.Length > MaxSearchLength)
            {
                errors.Add(new ValidationError("search", "search.tooLong",
                    $"Search term must not exceed {MaxSearchLength} characters."));
            }
        }

        if (request.SortBy is not null && !AllowedSortBy.Contains(request.SortBy))
        {
            errors.Add(new ValidationError("sortBy", "sortBy.invalid",
                "sortBy must be one of: createdAt, title, code."));
        }

        if (request.SortDir is not null && !AllowedSortDir.Contains(request.SortDir))
        {
            errors.Add(new ValidationError("sortDir", "sortDir.invalid",
                "sortDir must be 'asc' or 'desc'."));
        }

        return ValidationResult.FromErrors(errors);
    }
}
