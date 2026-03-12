using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Anizaki.Application.Features.Library.Contracts;

public interface ILibraryRepository
{
    Task<IReadOnlyCollection<CategoryItemDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    
    Task<(IReadOnlyCollection<DrawingItemDto> Items, int TotalItems)> GetDrawingsAsync(
        int page,
        int pageSize,
        string? categorySlug,
        string? search,
        string? sortBy,
        string? sortDir,
        CancellationToken cancellationToken);
        
    Task<GetDrawingDetailResponse?> GetDrawingDetailAsync(Guid id, CancellationToken cancellationToken);
    
    Task<GetDrawingPreviewResponse?> GetDrawingPreviewAsync(Guid id, CancellationToken cancellationToken);
}
