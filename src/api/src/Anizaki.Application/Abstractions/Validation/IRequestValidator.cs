namespace Anizaki.Application.Abstractions.Validation;

public interface IRequestValidator<in TRequest>
{
    ValidationResult Validate(TRequest request);
}
