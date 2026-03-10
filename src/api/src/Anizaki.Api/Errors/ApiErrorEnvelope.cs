namespace Anizaki.Api.Errors;

public sealed record ApiErrorEnvelope(
    string Error,
    string Message,
    string CorrelationId,
    object? Errors = null);
