using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class MarkSessionSuspiciousHandler : IRequestHandler<MarkSessionSuspiciousCommand, MarkSessionSuspiciousResponse>
{
    private readonly IRequestValidator<MarkSessionSuspiciousCommand> _validator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuthTokenService _authTokenService;

    public MarkSessionSuspiciousHandler(
        IRequestValidator<MarkSessionSuspiciousCommand> validator,
        ICurrentUserContext currentUserContext,
        IAuthTokenService authTokenService)
    {
        _validator = validator;
        _currentUserContext = currentUserContext;
        _authTokenService = authTokenService;
    }

    public async Task<MarkSessionSuspiciousResponse> HandleAsync(
        MarkSessionSuspiciousCommand request,
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

        var marked = await _authTokenService.MarkSessionSuspiciousAsync(
            _currentUserContext.UserId.Value,
            request.SessionId,
            request.Reason.Trim(),
            DateTime.UtcNow,
            cancellationToken);

        if (!marked)
        {
            throw new RequestValidationException([
                new ValidationError("sessionId", "session.notFound", "Session cannot be found.")
            ]);
        }

        return new MarkSessionSuspiciousResponse(
            Flagged: true,
            SessionId: request.SessionId);
    }
}

