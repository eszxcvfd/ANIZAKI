using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;

namespace Anizaki.Application.Features.Auth;

public sealed class GetMySessionsHandler : IRequestHandler<GetMySessionsQuery, GetMySessionsResponse>
{
    private readonly IRequestValidator<GetMySessionsQuery> _validator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuthTokenService _authTokenService;

    public GetMySessionsHandler(
        IRequestValidator<GetMySessionsQuery> validator,
        ICurrentUserContext currentUserContext,
        IAuthTokenService authTokenService)
    {
        _validator = validator;
        _currentUserContext = currentUserContext;
        _authTokenService = authTokenService;
    }

    public async Task<GetMySessionsResponse> HandleAsync(
        GetMySessionsQuery request,
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

        var sessions = await _authTokenService.ListSessionsAsync(_currentUserContext.UserId.Value, cancellationToken);
        var projection = sessions
            .OrderByDescending(session => session.IssuedAtUtc)
            .Select(ToSummary)
            .ToArray();

        return new GetMySessionsResponse(projection);
    }

    private static AuthSessionSummary ToSummary(AuthSessionRecord session)
    {
        return new AuthSessionSummary(
            SessionId: session.SessionId,
            IssuedAtUtc: session.IssuedAtUtc,
            AccessTokenExpiresAtUtc: session.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc: session.RefreshTokenExpiresAtUtc,
            Revoked: session.RevokedAtUtc.HasValue,
            RevokedAtUtc: session.RevokedAtUtc,
            RevocationReason: session.RevocationReason,
            Suspicious: session.IsSuspicious,
            SuspiciousMarkedAtUtc: session.SuspiciousMarkedAtUtc,
            SuspiciousReason: session.SuspiciousReason);
    }
}

