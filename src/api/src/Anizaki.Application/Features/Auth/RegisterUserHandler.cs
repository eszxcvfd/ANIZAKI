using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth;

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IRequestValidator<RegisterUserCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly ICredentialHasher _credentialHasher;
    private readonly IAuthTokenService _authTokenService;
    private readonly IEmailSender _emailSender;

    public RegisterUserHandler(
        IRequestValidator<RegisterUserCommand> validator,
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        ICredentialHasher credentialHasher,
        IAuthTokenService authTokenService,
        IEmailSender emailSender)
    {
        _validator = validator;
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _credentialHasher = credentialHasher;
        _authTokenService = authTokenService;
        _emailSender = emailSender;
    }

    public async Task<RegisterUserResponse> HandleAsync(
        RegisterUserCommand request,
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

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new RequestValidationException([
                new ValidationError("email", "email.duplicate", "An account with this email already exists.")
            ]);
        }

        var nowUtc = DateTime.UtcNow;
        var user = User.Create(Guid.NewGuid(), email, UserRole.User, nowUtc);
        var passwordHash = _credentialHasher.HashPassword(request.Password);
        await _userRepository.AddWithCredentialAsync(user, passwordHash, cancellationToken);

        var verificationToken = _authTokenService.IssueEmailVerificationToken(nowUtc);
        var emailVerificationToken = EmailVerificationToken.Issue(
            Guid.NewGuid(),
            user.Id,
            verificationToken.TokenHash,
            nowUtc,
            verificationToken.ExpiresAtUtc);
        await _userTokenRepository.AddEmailVerificationTokenAsync(emailVerificationToken, cancellationToken);

        await _emailSender.SendAsync(
            new AuthEmailMessage(
                user.Email,
                "Verify your email",
                $"Use this token to verify your email: {verificationToken.PlainTextToken}",
                null),
            cancellationToken);

        return new RegisterUserResponse(
            user.Id,
            user.Email.Value,
            VerificationRequired: true,
            VerificationTokenExpiresAtUtc: verificationToken.ExpiresAtUtc);
    }
}
