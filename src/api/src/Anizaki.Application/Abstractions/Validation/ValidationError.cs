namespace Anizaki.Application.Abstractions.Validation;

public sealed record ValidationError(string Field, string Code, string Message);
