namespace Anizaki.Application.Features.Auth.Contracts;

public sealed record ResetPasswordResponse(
    bool PasswordReset,
    DateTime PasswordChangedAtUtc);

