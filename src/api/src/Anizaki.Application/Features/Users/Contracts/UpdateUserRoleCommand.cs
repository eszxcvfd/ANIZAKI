using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Users.Contracts;

public sealed record UpdateUserRoleCommand(Guid UserId, string Role) : IRequest<UpdateUserRoleResponse>;
