using Anizaki.Application.Abstractions.Messaging;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetCategoriesQuery : IRequest<GetCategoriesResponse>;
