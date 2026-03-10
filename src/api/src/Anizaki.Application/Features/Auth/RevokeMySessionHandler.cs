using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class RevokeMySessionHandler : IRequestHandler<RevokeMySessionCommand, RevokeMySessionResponse>
{
    private readonly IRequestValidator<RevokeMySessionCommand> _validator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuthTokenService _authTokenService;

    public RevokeMySessionHandler(
        IRequestValidator<RevokeMySessionCommand> validator,
        ICurrentUserContext currentUserContext,
        IAuthTokenService authTokenService)
    {
        _validator = validator;
        _currentUserContext = currentUserContext;
        _authTokenService = authTokenService;
    }

    public async Task<RevokeMySessionResponse> HandleAsync(
        RevokeMySessionCommand request,
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

        var revoked = await _authTokenService.RevokeSessionByIdAsync(
            _currentUserContext.UserId.Value,
            request.SessionId,
            request.Reason,
            DateTime.UtcNow,
            cancellationToken);

        if (!revoked)
        {
            throw new RequestValidationException([
                new ValidationError("sessionId", "session.notFound", "Session cannot be found.")
            ]);
        }

        return new RevokeMySessionResponse(
            Revoked: true,
            SessionId: request.SessionId);
    }
}

