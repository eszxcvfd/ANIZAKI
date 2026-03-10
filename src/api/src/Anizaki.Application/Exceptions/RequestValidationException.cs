using Anizaki.Application.Abstractions.Validation;

namespace Anizaki.Application.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IReadOnlyCollection<ValidationError> errors)
        : base("Request validation failed.")
    {
        Errors = errors;
    }

    public IReadOnlyCollection<ValidationError> Errors { get; }
}
