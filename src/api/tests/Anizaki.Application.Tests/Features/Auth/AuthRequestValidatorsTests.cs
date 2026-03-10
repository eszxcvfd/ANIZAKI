using Anizaki.Application.Features.Auth;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Tests.Features.Auth;

public class AuthRequestValidatorsTests
{
    [Fact]
    public void RegisterValidator_WithInvalidPayload_ShouldReturnExpectedErrors()
    {
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand(" ", "123");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "email.empty");
        Assert.Contains(result.Errors, error => error.Code == "password.tooShort");
    }

    [Fact]
    public void LoginValidator_WithMissingFields_ShouldReturnExpectedErrors()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("", "");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "email.empty");
        Assert.Contains(result.Errors, error => error.Code == "password.empty");
    }

    [Fact]
    public void LogoutValidator_WithWhitespaceRefreshToken_ShouldReturnError()
    {
        var validator = new LogoutCommandValidator();
        var command = new LogoutCommand("   ");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("refreshToken.empty", error.Code);
    }

    [Fact]
    public void ForgotPasswordValidator_WithMissingEmail_ShouldReturnError()
    {
        var validator = new ForgotPasswordCommandValidator();
        var command = new ForgotPasswordCommand("");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("email.empty", error.Code);
    }

    [Fact]
    public void ResetPasswordValidator_WithWeakPayload_ShouldReturnExpectedErrors()
    {
        var validator = new ResetPasswordCommandValidator();
        var command = new ResetPasswordCommand("", "123");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "token.empty");
        Assert.Contains(result.Errors, error => error.Code == "newPassword.tooShort");
    }

    [Fact]
    public void VerifyEmailValidator_WithMissingToken_ShouldReturnError()
    {
        var validator = new VerifyEmailCommandValidator();
        var command = new VerifyEmailCommand(" ");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("token.empty", error.Code);
    }

    [Fact]
    public void RevokeMySessionValidator_WithEmptySessionId_ShouldReturnError()
    {
        var validator = new RevokeMySessionCommandValidator();
        var command = new RevokeMySessionCommand(Guid.Empty, "manual");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "session.required");
    }

    [Fact]
    public void MarkSessionSuspiciousValidator_WithMissingReason_ShouldReturnError()
    {
        var validator = new MarkSessionSuspiciousCommandValidator();
        var command = new MarkSessionSuspiciousCommand(Guid.NewGuid(), " ");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "session.reasonRequired");
    }
}
