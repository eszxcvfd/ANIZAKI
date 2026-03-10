using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth;

public sealed class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IRequestValidator<VerifyEmailCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IAuthTokenService _authTokenService;
    private readonly ISecurityAuditLogger _securityAuditLogger;

    public VerifyEmailHandler(
        IRequestValidator<VerifyEmailCommand> validator,
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IAuthTokenService authTokenService,
        ISecurityAuditLogger securityAuditLogger)
    {
        _validator = validator;
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _authTokenService = authTokenService;
        _securityAuditLogger = securityAuditLogger;
    }

    public async Task<VerifyEmailResponse> HandleAsync(
        VerifyEmailCommand request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var tokenHash = _authTokenService.HashOneTimeToken(request.Token);
        var token = await _userTokenRepository.GetEmailVerificationTokenByHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalid", "Verification token is invalid.")
            ]);
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalid", "Verification token is invalid.")
            ]);
        }

        if (user.IsEmailVerified)
        {
            throw new RequestValidationException([
                new ValidationError("email", "email.alreadyVerified", "Email is already verified.")
            ]);
        }

        var nowUtc = DateTime.UtcNow;
        if (!token.CanBeUsedAt(nowUtc))
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalidOrExpired", "Verification token is invalid or expired.")
            ]);
        }

        EmailVerificationToken usedToken;
        try
        {
            usedToken = user.VerifyEmail(token, nowUtc);
        }
        catch (DomainException exception)
        {
            throw new RequestValidationException([
                new ValidationError("token", "token.invalid", exception.Message)
            ]);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userTokenRepository.UpdateEmailVerificationTokenAsync(usedToken, cancellationToken);
        await _securityAuditLogger.EmailVerifiedAsync(user.Id, user.Email.Value, nowUtc, cancellationToken);

        return new VerifyEmailResponse(
            Verified: true,
            VerifiedAtUtc: user.EmailVerifiedAtUtc ?? nowUtc,
            Email: user.Email.Value);
    }
}
