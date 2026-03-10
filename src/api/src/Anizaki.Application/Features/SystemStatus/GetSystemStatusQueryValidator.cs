using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Features.SystemStatus.Contracts;

namespace Anizaki.Application.Features.SystemStatus;

public sealed class GetSystemStatusQueryValidator : IRequestValidator<GetSystemStatusQuery>
{
    public const int MaxCorrelationIdLength = 64;

    public ValidationResult Validate(GetSystemStatusQuery request)
    {
        if (request is null)
        {
            return ValidationResult.FromErrors(
            [
                new ValidationError("request", "request.null", "Request payload is required.")
            ]);
        }

        if (request.CorrelationId is null)
        {
            return ValidationResult.Success;
        }

        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            return ValidationResult.FromErrors(
            [
                new ValidationError(
                    "correlationId",
                    "correlationId.empty",
                    "CorrelationId must be omitted or contain non-whitespace characters.")
            ]);
        }

        if (request.CorrelationId.Length > MaxCorrelationIdLength)
        {
            return ValidationResult.FromErrors(
            [
                new ValidationError(
                    "correlationId",
                    "correlationId.tooLong",
                    $"CorrelationId must not exceed {MaxCorrelationIdLength} characters.")
            ]);
        }

        return ValidationResult.Success;
    }
}
