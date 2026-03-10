using Anizaki.Application.Abstractions.Messaging;
using Anizaki.Application.Abstractions.Validation;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Auth.Contracts;
using Anizaki.Application.Features.Users.Contracts;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.Users;

namespace Anizaki.Application.Features.Users;

public sealed class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand, UpdateUserRoleResponse>
{
    private readonly IRequestValidator<UpdateUserRoleCommand> _validator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly ISecurityAuditLogger _securityAuditLogger;

    public UpdateUserRoleHandler(
        IRequestValidator<UpdateUserRoleCommand> validator,
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        ISecurityAuditLogger securityAuditLogger)
    {
        _validator = validator;
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _securityAuditLogger = securityAuditLogger;
    }

    public async Task<UpdateUserRoleResponse> HandleAsync(
        UpdateUserRoleCommand request,
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

        if (_currentUserContext.Role != UserRole.Admin)
        {
            throw new RequestValidationException([
                new ValidationError("auth", "auth.forbidden", "Current user does not have permission to update roles.")
            ]);
        }

        UserRole targetRole;
        try
        {
            targetRole = UserRole.From(request.Role);
        }
        catch (DomainException exception)
        {
            throw new RequestValidationException([
                new ValidationError("role", "role.invalid", exception.Message)
            ]);
        }

        var targetUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (targetUser is null)
        {
            throw new RequestValidationException([
                new ValidationError("userId", "user.notFound", "Target user does not exist.")
            ]);
        }

        var previousRole = targetUser.Role.Value;

        try
        {
            targetUser.ChangeRole(targetRole, DateTime.UtcNow);
        }
        catch (DomainException exception)
        {
            throw new RequestValidationException([
                new ValidationError("role", "role.invalidTransition", exception.Message)
            ]);
        }

        await _userRepository.UpdateAsync(targetUser, cancellationToken);
        await _securityAuditLogger.UserRoleChangedAsync(
            actorUserId: _currentUserContext.UserId.Value,
            targetUserId: targetUser.Id,
            previousRole: previousRole,
            nextRole: targetUser.Role.Value,
            occurredAtUtc: targetUser.UpdatedAtUtc,
            cancellationToken);

        return new UpdateUserRoleResponse(
            UserId: targetUser.Id,
            Role: targetUser.Role.Value,
            UpdatedAtUtc: targetUser.UpdatedAtUtc);
    }
}
