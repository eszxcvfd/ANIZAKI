using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.Users.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Users;

public sealed class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, MyProfileResponse>
{
    private readonly IRequestValidator<UpdateMyProfileCommand> _validator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;

    public UpdateMyProfileHandler(
        IRequestValidator<UpdateMyProfileCommand> validator,
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository)
    {
        _validator = validator;
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
    }

    public async Task<MyProfileResponse> HandleAsync(
        UpdateMyProfileCommand request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.UserId.HasValue)
        {
            throw new RequestValidationException([
                new ValidationError("auth", "auth.unauthenticated", "Current user is not authenticated.")
            ]);
        }

        var user = await _userRepository.GetByIdAsync(_currentUserContext.UserId.Value, cancellationToken);
        if (user is null)
        {
            throw new RequestValidationException([
                new ValidationError("auth", "auth.userNotFound", "Current user cannot be found.")
            ]);
        }

        UserEmail newEmail;
        try
        {
            newEmail = UserEmail.From(request.Email);
        }
        catch (DomainException exception)
        {
            throw new RequestValidationException([
                new ValidationError("email", "email.invalid", exception.Message)
            ]);
        }

        if (user.Email != newEmail)
        {
            user.ChangeEmail(newEmail, DateTime.UtcNow);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return new MyProfileResponse(
            UserId: user.Id,
            Email: user.Email.Value,
            Role: user.Role.Value,
            EmailVerified: user.IsEmailVerified,
            EmailVerifiedAtUtc: user.EmailVerifiedAtUtc,
            CreatedAtUtc: user.CreatedAtUtc,
            UpdatedAtUtc: user.UpdatedAtUtc);
    }
}

