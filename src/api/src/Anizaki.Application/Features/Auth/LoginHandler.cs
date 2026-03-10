using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth;

public sealed class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IRequestValidator<LoginCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly ICredentialHasher _credentialHasher;
    private readonly IAuthTokenService _authTokenService;
    private readonly ILoginAttemptLockoutService _loginAttemptLockoutService;
    private readonly ISecurityAuditLogger _securityAuditLogger;

    public LoginHandler(
        IRequestValidator<LoginCommand> validator,
        IUserRepository userRepository,
        ICredentialHasher credentialHasher,
        IAuthTokenService authTokenService,
        ILoginAttemptLockoutService loginAttemptLockoutService,
        ISecurityAuditLogger securityAuditLogger)
    {
        _validator = validator;
        _userRepository = userRepository;
        _credentialHasher = credentialHasher;
        _authTokenService = authTokenService;
        _loginAttemptLockoutService = loginAttemptLockoutService;
        _securityAuditLogger = securityAuditLogger;
    }

    public async Task<LoginResponse> HandleAsync(
        LoginCommand request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        UserEmail email;
        try
        {
            email = UserEmail.From(request.Email);
        }
        catch (DomainException exception)
        {
            throw new RequestValidationException([
                new ValidationError("email", "email.invalid", exception.Message)
            ]);
        }

        var nowUtc = DateTime.UtcNow;
        var lockoutState = await _loginAttemptLockoutService.GetStateAsync(email.Value, nowUtc, cancellationToken);
        if (lockoutState.IsLockedOut)
        {
            await _securityAuditLogger.LoginFailedAsync(
                email.Value,
                nowUtc,
                accountLocked: true,
                lockoutState.LockedUntilUtc,
                cancellationToken);

            throw new RequestThrottledException(
                "Too many failed sign-in attempts. Please try again later.",
                lockoutState.LockedUntilUtc);
        }

        var snapshot = await _userRepository.GetCredentialSnapshotByEmailAsync(email, cancellationToken);
        if (snapshot is null || !_credentialHasher.VerifyPassword(request.Password, snapshot.PasswordHash))
        {
            var failedAttemptState = await _loginAttemptLockoutService.RegisterFailedAttemptAsync(
                email.Value,
                nowUtc,
                cancellationToken);

            await _securityAuditLogger.LoginFailedAsync(
                email.Value,
                nowUtc,
                failedAttemptState.IsLockedOut,
                failedAttemptState.LockedUntilUtc,
                cancellationToken);

            if (failedAttemptState.IsLockedOut)
            {
                throw new RequestThrottledException(
                    "Too many failed sign-in attempts. Please try again later.",
                    failedAttemptState.LockedUntilUtc);
            }

            throw new RequestValidationException([
                new ValidationError("credentials", "auth.invalidCredentials", "Credentials are invalid.")
            ]);
        }

        await _loginAttemptLockoutService.ResetAsync(email.Value, cancellationToken);
        var tokens = _authTokenService.IssueSessionTokens(snapshot.User, nowUtc);
        await _authTokenService.RecordSessionAsync(
            snapshot.User.Id,
            tokens,
            nowUtc,
            cancellationToken);
        return new LoginResponse(
            snapshot.User.Id,
            snapshot.User.Email.Value,
            snapshot.User.Role.Value,
            tokens);
    }
}
