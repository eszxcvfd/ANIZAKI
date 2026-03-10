using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Auth;

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IRequestValidator<ForgotPasswordCommand> _validator;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IAuthTokenService _authTokenService;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordHandler(
        IRequestValidator<ForgotPasswordCommand> validator,
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IAuthTokenService authTokenService,
        IEmailSender emailSender)
    {
        _validator = validator;
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _authTokenService = authTokenService;
        _emailSender = emailSender;
    }

    public async Task<ForgotPasswordResponse> HandleAsync(
        ForgotPasswordCommand request,
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

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Deliberately return success to avoid account enumeration.
            return new ForgotPasswordResponse(Accepted: true);
        }

        var nowUtc = DateTime.UtcNow;
        var issuedToken = _authTokenService.IssuePasswordResetToken(nowUtc);
        var resetToken = PasswordResetToken.Issue(
            Guid.NewGuid(),
            user.Id,
            issuedToken.TokenHash,
            nowUtc,
            issuedToken.ExpiresAtUtc);

        await _userTokenRepository.AddPasswordResetTokenAsync(resetToken, cancellationToken);
        await _emailSender.SendAsync(
            new AuthEmailMessage(
                user.Email,
                "Reset your password",
                $"Use this token to reset your password: {issuedToken.PlainTextToken}",
                null),
            cancellationToken);

        return new ForgotPasswordResponse(Accepted: true);
    }
}

