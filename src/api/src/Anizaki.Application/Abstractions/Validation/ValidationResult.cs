namespace Anizaki.Application.Abstractions.Validation;

public sealed class ValidationResult
{
    private ValidationResult(IReadOnlyCollection<ValidationError> errors)
    {
        Errors = errors;
    }

    public static ValidationResult Success { get; } = new(Array.Empty<ValidationError>());

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static ValidationResult FromErrors(IEnumerable<ValidationError> errors)
    {
        var errorList = errors
            .Where(error => error is not null)
            .ToArray();

        return errorList.Length == 0
            ? Success
            : new ValidationResult(errorList);
    }
}
