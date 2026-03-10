using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Users.Contracts;

public sealed record UpdateMyProfileCommand(string Email) : IRequest<MyProfileResponse>;

