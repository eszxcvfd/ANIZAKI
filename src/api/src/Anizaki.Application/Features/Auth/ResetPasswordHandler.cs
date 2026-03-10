using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IRequestValidator<ResetPasswordCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly ICredentialHasher _credentialHasher;
    private readonly IAuthTokenService _authTokenService;
    private readonly ISecurityAuditLogger _securityAuditLogger;

    public ResetPasswordHandler(
        IRequestValidator<ResetPasswordCommand> validator,
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        ICredentialHasher credentialHasher,
        IAuthTokenService authTokenService,
        ISecurityAuditLogger securityAuditLogger)
    {
        _validator = validator;
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _credentialHasher = credentialHasher;
        _authTokenService = authTokenService;
        _securityAuditLogger = securityAuditLogger;
    }

    public async Task<ResetPasswordResponse> HandleAsync(
        ResetPasswordCommand request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var tokenHash = _authTokenService.HashOneTimeToken(request.Token);
        var token = await _userTokenRepository.GetPasswordResetTokenByHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalid", "Password reset token is invalid.")
            ]);
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalid", "Password reset token is invalid.")
            ]);
        }

        var nowUtc = DateTime.UtcNow;
        if (!token.CanBeUsedAt(nowUtc))
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalidOrExpired", "Password reset token is invalid or expired.")
            ]);
        }

        PasswordResetToken usedToken;
        try
        {
            usedToken = user.ApplyPasswordReset(token, nowUtc);
        }
        catch (DomainException exception)
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalid", exception.Message)
            ]);
        }

        var passwordHash = _credentialHasher.HashPassword(request.NewPassword);
        await _userRepository.SetPasswordHashAsync(user.Id, passwordHash, cancellationToken);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userTokenRepository.UpdatePasswordResetTokenAsync(usedToken, cancellationToken);
        await _securityAuditLogger.PasswordResetAsync(user.Id, nowUtc, cancellationToken);

        return new ResetPasswordResponse(
            PasswordReset: true,
            PasswordChangedAtUtc: user.PasswordChangedAtUtc ?? nowUtc);
    }
}
