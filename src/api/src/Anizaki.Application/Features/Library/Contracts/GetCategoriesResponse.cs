using System;
using System.Collections.Generic;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetCategoriesResponse(
    IReadOnlyCollection<CategoryItemDto> Items);

public sealed record CategoryItemDto(
    Guid Id,
    string Name,
    string Slug,
    int Order,
    int DrawingCount);
