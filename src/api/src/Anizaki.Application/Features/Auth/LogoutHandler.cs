using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, LogoutResponse>
{
    private readonly IRequestValidator<LogoutCommand> _validator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuthTokenService _authTokenService;

    public LogoutHandler(
        IRequestValidator<LogoutCommand> validator,
        ICurrentUserContext currentUserContext,
        IAuthTokenService authTokenService)
    {
        _validator = validator;
        _currentUserContext = currentUserContext;
        _authTokenService = authTokenService;
    }

    public async Task<LogoutResponse> HandleAsync(
        LogoutCommand request,
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

        await _authTokenService.RevokeSessionAsync(
            _currentUserContext.UserId.Value,
            request.RefreshToken,
            DateTime.UtcNow,
            cancellationToken);

        return new LogoutResponse(Revoked: true);
    }
}

