using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anizaki.Application.Exceptions;
using Anizaki.Application.Features.Library;
using Anizaki.Application.Features.Library.Contracts;
using Xunit;

namespace Anizaki.Application.Tests.Features.Library;

// ─── GetCategories ────────────────────────────────────────────────────────────

public class GetCategoriesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOrderedCategories()
    {
        var handler = new GetCategoriesHandler(StubRepo.Build(), new GetCategoriesQueryValidator());

        var result = await handler.HandleAsync(new GetCategoriesQuery());

        Assert.Equal(4, result.Items.Count);
        var orders = result.Items.Select(c => c.Order).ToList();
        Assert.Equal(orders.OrderBy(x => x).ToList(), orders);
    }

    [Fact]
    public async Task HandleAsync_CountsMatchDrawingsPerCategory()
    {
        var handler = new GetCategoriesHandler(StubRepo.Build(), new GetCategoriesQueryValidator());

        var result = await handler.HandleAsync(new GetCategoriesQuery());

        Assert.Equal(3, result.Items.Single(c => c.Slug == "kien-truc").DrawingCount);
        Assert.Equal(2, result.Items.Single(c => c.Slug == "ket-cau").DrawingCount);
        Assert.Equal(1, result.Items.Single(c => c.Slug == "co-dien").DrawingCount);
        Assert.Equal(0, result.Items.Single(c => c.Slug == "noi-that").DrawingCount);
    }
}

// ─── GetDrawingList ───────────────────────────────────────────────────────────

public class GetDrawingListHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAllDrawingsPaginated()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null));

        Assert.Equal(6, result.Pagination.TotalItems);
        Assert.Equal(6, result.Items.Count);
        Assert.Equal(1, result.Pagination.TotalPages);
        Assert.Equal(1, result.Pagination.Page);
        Assert.Equal(10, result.Pagination.PageSize);
    }

    [Fact]
    public async Task HandleAsync_FiltersByCategory_kienTruc()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, "kien-truc", null));

        Assert.Equal(3, result.Pagination.TotalItems);
        Assert.All(result.Items, d => Assert.Equal("kien-truc", d.CategorySlug));
    }

    [Fact]
    public async Task HandleAsync_EmptyCategory_ReturnsEmptyList()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, "noi-that", null));

        Assert.Equal(0, result.Pagination.TotalItems);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Pagination.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_SearchByTitle_Mat_ReturnsTwoResults()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        // "Mặt" matches "Mặt Bằng Tầng Trệt" and "Mặt Cắt Đứng Trục A-D"
        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, "Mặt"));

        Assert.Equal(2, result.Pagination.TotalItems);
        Assert.All(result.Items, d => Assert.Contains("Mặt", d.Title));
    }

    [Fact]
    public async Task HandleAsync_SearchByCode_STR_ReturnsTwoResults()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, "STR"));

        Assert.Equal(2, result.Pagination.TotalItems);
        Assert.All(result.Items, d => Assert.StartsWith("STR", d.Code));
    }

    [Fact]
    public async Task HandleAsync_PaginatesCorrectly_TwoPer_Page()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var page1 = await handler.HandleAsync(new GetDrawingListQuery(1, 2, null, null));
        var page2 = await handler.HandleAsync(new GetDrawingListQuery(2, 2, null, null));
        var page3 = await handler.HandleAsync(new GetDrawingListQuery(3, 2, null, null));

        Assert.Equal(6, page1.Pagination.TotalItems);
        Assert.Equal(3, page1.Pagination.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(2, page3.Items.Count);

        var allIds = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(d => d.Id).ToList();
        Assert.Equal(6, allIds.Distinct().Count());
    }

    [Theory]
    [InlineData(0, 10)]   // page < 1
    [InlineData(1, 0)]    // pageSize < 1
    [InlineData(1, 101)]  // pageSize > 100
    public async Task HandleAsync_InvalidPagination_ThrowsValidationException(int page, int pageSize)
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetDrawingListQuery(page, pageSize, null, null)));
    }

    [Fact]
    public async Task HandleAsync_BlankCategory_ThrowsValidationException()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetDrawingListQuery(1, 10, "   ", null)));

        Assert.Contains(ex.Errors, e => e.Code == "category.empty");
    }

    [Fact]
    public async Task HandleAsync_BlankSearch_ThrowsValidationException()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetDrawingListQuery(1, 10, null, "   ")));

        Assert.Contains(ex.Errors, e => e.Code == "search.empty");
    }

    [Fact]
    public async Task HandleAsync_CategoryTooLong_ThrowsValidationException()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());
        var longCategory = new string('a', 101);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetDrawingListQuery(1, 10, longCategory, null)));

        Assert.Contains(ex.Errors, e => e.Code == "category.tooLong");
    }
}

// ─── GetDrawingDetail ─────────────────────────────────────────────────────────

public class GetDrawingDetailHandlerTests
{
    private static readonly Guid KnownArc001Id = new("d1000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task HandleAsync_WithKnownId_ReturnsDetail()
    {
        var handler = new GetDrawingDetailHandler(StubRepo.Build(), new GetDrawingDetailQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingDetailQuery(KnownArc001Id));

        Assert.Equal(KnownArc001Id, result.Id);
        Assert.Equal("ARC-001", result.Code);
        Assert.Equal("kien-truc", result.CategorySlug);
        Assert.Equal("published", result.Status);
        Assert.NotNull(result.FileInfo);
        Assert.Equal("image/png", result.FileInfo.MimeType);
        Assert.Equal("available", result.FileInfo.PreviewAvailability);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownId_ThrowsResourceNotFoundException()
    {
        var handler = new GetDrawingDetailHandler(StubRepo.Build(), new GetDrawingDetailQueryValidator());

        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.HandleAsync(new GetDrawingDetailQuery(Guid.NewGuid())));

        Assert.Equal("Drawing", ex.ResourceType);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyGuid_ThrowsValidationException()
    {
        var handler = new GetDrawingDetailHandler(StubRepo.Build(), new GetDrawingDetailQueryValidator());

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetDrawingDetailQuery(Guid.Empty)));

        Assert.Contains(ex.Errors, e => e.Code == "drawingId.empty");
        Assert.DoesNotContain(ex.Errors, e => e.Field == "blah"); // sanity check
    }

    [Fact]
    public async Task HandleAsync_CadDrawing_HasUnavailablePreviewInFileInfo()
    {
        var handler = new GetDrawingDetailHandler(StubRepo.Build(), new GetDrawingDetailQueryValidator());
        // Drawing 3 = ARC-003, CAD file
        var id = new Guid("d1000000-0000-0000-0000-000000000003");

        var result = await handler.HandleAsync(new GetDrawingDetailQuery(id));

        Assert.Equal("unavailable", result.FileInfo.PreviewAvailability);
    }
}

// ─── GetDrawingPreview ────────────────────────────────────────────────────────

public class GetDrawingPreviewHandlerTests
{
    private static readonly Guid Arc001Id = new("d1000000-0000-0000-0000-000000000001"); // image
    private static readonly Guid Arc002Id = new("d1000000-0000-0000-0000-000000000002"); // pdf
    private static readonly Guid Arc003Id = new("d1000000-0000-0000-0000-000000000003"); // unavailable
    private static readonly Guid Str001Id = new("d1000000-0000-0000-0000-000000000004"); // generating

    [Fact]
    public async Task HandleAsync_AvailableImage_ReturnsPreviewPayload()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingPreviewQuery(Arc001Id));

        Assert.Equal("available", result.PreviewAvailability);
        Assert.Equal("image", result.PreviewType);
        Assert.NotNull(result.PreviewUrl);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task HandleAsync_AvailablePdf_ReturnsPreviewPayload()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingPreviewQuery(Arc002Id));

        Assert.Equal("available", result.PreviewAvailability);
        Assert.Equal("pdf", result.PreviewType);
        Assert.NotNull(result.PreviewUrl);
    }

    [Fact]
    public async Task HandleAsync_Unavailable_ReturnsUnavailableWithMessage()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingPreviewQuery(Arc003Id));

        Assert.Equal("unavailable", result.PreviewAvailability);
        Assert.Null(result.PreviewType);
        Assert.Null(result.PreviewUrl);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task HandleAsync_Generating_ReturnsGeneratingWithMessage()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingPreviewQuery(Str001Id));

        Assert.Equal("generating", result.PreviewAvailability);
        Assert.Null(result.PreviewType);
        Assert.Null(result.PreviewUrl);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownId_ThrowsResourceNotFoundException()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => handler.HandleAsync(new GetDrawingPreviewQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyGuid_ThrowsValidationException()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        await Assert.ThrowsAsync<RequestValidationException>(
            () => handler.HandleAsync(new GetDrawingPreviewQuery(Guid.Empty)));
    }

    [Fact]
    public async Task HandleAsync_DrawingIdIsReturnedInResponse()
    {
        var handler = new GetDrawingPreviewHandler(StubRepo.Build(), new GetDrawingPreviewQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingPreviewQuery(Arc001Id));

        Assert.Equal(Arc001Id, result.DrawingId);
    }
}

// ─── Self-contained stub (no Infrastructure dependency) ───────────────────────

/// <summary>
/// Pure in-process stub that mirrors the InMemoryLibraryRepository fixture,
/// without referencing the Infrastructure project.
/// </summary>
internal static class StubRepo
{
    public static ILibraryRepository Build() => new StubLibraryRepository();
}

internal sealed class StubLibraryRepository : ILibraryRepository
{
    private static readonly Guid CatKienTrucId = new("01000000-0000-0000-0000-000000000001");
    private static readonly Guid CatKetCauId   = new("01000000-0000-0000-0000-000000000002");
    private static readonly Guid CatCoDienId   = new("01000000-0000-0000-0000-000000000003");
    private static readonly Guid CatNoiThatId  = new("01000000-0000-0000-0000-000000000004");

    private sealed record DrawingRecord(
        Guid Id, string Title, string Code, string? Description,
        Guid CategoryId, string CategorySlug, string CategoryName,
        string Status, DateTime CreatedAtUtc,
        string FileName, string MimeType, long SizeBytes,
        string Checksum, DateTime UploadedAtUtc,
        string PreviewAvailability, string? PreviewType,
        string? PreviewUrl, string? PreviewMessage,
        IReadOnlyCollection<string> Tags);

    private static readonly DrawingRecord[] _drawings =
    [
        new(new Guid("d1000000-0000-0000-0000-000000000001"),
            "Mặt Bằng Tầng Trệt", "ARC-001", "Bản vẽ mặt bằng tầng trệt.",
            CatKienTrucId, "kien-truc", "Kiến Trúc", "published",
            new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            "floorplan.png", "image/png", 204800,
            "sha256:abc123def456", new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            "available", "image", "/mock/floorplan.png", null,
            ["mặt bằng", "tầng trệt"]),

        new(new Guid("d1000000-0000-0000-0000-000000000002"),
            "Mặt Cắt Đứng Trục A-D", "ARC-002", "Mặt cắt đứng theo trục A-D.",
            CatKienTrucId, "kien-truc", "Kiến Trúc", "published",
            new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc),
            "section.pdf", "application/pdf", 512000,
            "sha256:def789abc012", new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc),
            "available", "pdf", "/mock/section.pdf", null,
            ["mặt cắt"]),

        new(new Guid("d1000000-0000-0000-0000-000000000003"),
            "Bản Vẽ Chi Tiết Mái", "ARC-003", null,
            CatKienTrucId, "kien-truc", "Kiến Trúc", "published",
            new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            "roof-detail.dwg", "application/acad", 1048576,
            "sha256:cad001moo002", new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            "unavailable", null, null, "Định dạng CAD (.dwg) không hỗ trợ xem trước.",
            ["mái"]),

        new(new Guid("d1000000-0000-0000-0000-000000000004"),
            "Cốt Thép Sàn Tầng 2", "STR-001", "Bản vẽ bố trí cốt thép.",
            CatKetCauId, "ket-cau", "Kết Cấu", "published",
            new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            "rebar-t2.pdf", "application/pdf", 768000,
            "sha256:rebar001xx99", new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            "generating", null, null, "Bản xem trước đang được tạo.",
            ["cốt thép"]),

        new(new Guid("d1000000-0000-0000-0000-000000000005"),
            "Khung Thép Chịu Lực", "STR-002", "Bộ bản vẽ khung thép.",
            CatKetCauId, "ket-cau", "Kết Cấu", "published",
            new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            "frame.rar", "application/x-rar-compressed", 2097152,
            "sha256:rar001framex", new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            "unavailable", null, null, "Định dạng nén (.rar) không hỗ trợ xem trước.",
            ["khung thép"]),

        new(new Guid("d1000000-0000-0000-0000-000000000006"),
            "Hệ Thống Điện Chiếu Sáng", "MEP-001", "Thiết kế sơ bộ hệ thống chiếu sáng.",
            CatCoDienId, "co-dien", "Cơ Điện", "draft",
            new DateTime(2026, 2, 10, 14, 0, 0, DateTimeKind.Utc),
            "lighting.pdf", "application/pdf", 315000,
            "sha256:light001pdff", new DateTime(2026, 2, 10, 14, 0, 0, DateTimeKind.Utc),
            "available", "pdf", "/mock/lighting.pdf", null,
            ["điện", "chiếu sáng"]),
    ];

    private static readonly IReadOnlyList<CategoryItemDto> _categories;

    static StubLibraryRepository()
    {
        var counts = _drawings.GroupBy(d => d.CategoryId).ToDictionary(g => g.Key, g => g.Count());
        _categories =
        [
            new(CatKienTrucId, "Kiến Trúc", "kien-truc", 1, counts.GetValueOrDefault(CatKienTrucId, 0)),
            new(CatKetCauId,   "Kết Cấu",   "ket-cau",   2, counts.GetValueOrDefault(CatKetCauId,   0)),
            new(CatCoDienId,   "Cơ Điện",   "co-dien",   3, counts.GetValueOrDefault(CatCoDienId,   0)),
            new(CatNoiThatId,  "Nội Thất",  "noi-that",  4, counts.GetValueOrDefault(CatNoiThatId,  0)),
        ];
    }

    public Task<IReadOnlyCollection<CategoryItemDto>> GetCategoriesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyCollection<CategoryItemDto>>(_categories);

    public Task<(IReadOnlyCollection<DrawingItemDto> Items, int TotalItems)> GetDrawingsAsync(
        int page, int pageSize, string? categorySlug, string? search,
        string? sortBy, string? sortDir,
        CancellationToken ct)
    {
        var query = _drawings.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(d => d.CategorySlug.Equals(categorySlug.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.Code.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var all = (sortBy switch
        {
            "title"     => desc ? query.OrderByDescending(d => d.Title)        : query.OrderBy(d => d.Title),
            "code"      => desc ? query.OrderByDescending(d => d.Code)         : query.OrderBy(d => d.Code),
            "createdAt" => desc ? query.OrderByDescending(d => d.CreatedAtUtc) : query.OrderBy(d => d.CreatedAtUtc),
            _           => query.OrderBy(d => d.CreatedAtUtc),
        }).ToList();

        var sliced = all.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DrawingItemDto(d.Id, d.Title, d.Code, d.CategorySlug, d.CategoryName,
                d.Status, d.CreatedAtUtc, d.PreviewUrl))
            .ToList();
        return Task.FromResult<(IReadOnlyCollection<DrawingItemDto>, int)>((sliced, all.Count));
    }

    public Task<GetDrawingDetailResponse?> GetDrawingDetailAsync(Guid id, CancellationToken ct)
    {
        var d = _drawings.FirstOrDefault(x => x.Id == id);
        if (d is null) return Task.FromResult<GetDrawingDetailResponse?>(null);
        return Task.FromResult<GetDrawingDetailResponse?>(new GetDrawingDetailResponse(
            d.Id, d.Title, d.Code, d.Description, d.CategorySlug, d.CategoryName,
            d.Status, d.Tags, d.CreatedAtUtc, null,
            new FileInfoDetailDto(d.FileName, d.MimeType, d.SizeBytes, d.Checksum,
                d.UploadedAtUtc, d.PreviewAvailability)));
    }

    public Task<GetDrawingPreviewResponse?> GetDrawingPreviewAsync(Guid id, CancellationToken ct)
    {
        var d = _drawings.FirstOrDefault(x => x.Id == id);
        if (d is null) return Task.FromResult<GetDrawingPreviewResponse?>(null);
        return Task.FromResult<GetDrawingPreviewResponse?>(
            new GetDrawingPreviewResponse(d.Id, d.PreviewAvailability, d.PreviewType, d.PreviewUrl, d.PreviewMessage));
    }
}

// ─── GetDrawingList — Sort ────────────────────────────────────────────────────

public class GetDrawingListSortHandlerTests
{
    [Fact]
    public async Task HandleAsync_SortByTitle_Asc_ReturnsTitlesInAlphabeticalOrder()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null, "title", "asc"));

        var titles = result.Items.Select(d => d.Title).ToList();
        Assert.Equal(titles.OrderBy(t => t, StringComparer.Ordinal).ToList(), titles);
    }

    [Fact]
    public async Task HandleAsync_SortByTitle_Desc_ReturnsTitlesInReverseAlphabeticalOrder()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null, "title", "desc"));

        var titles = result.Items.Select(d => d.Title).ToList();
        Assert.Equal(titles.OrderByDescending(t => t, StringComparer.Ordinal).ToList(), titles);
    }

    [Fact]
    public async Task HandleAsync_SortByCreatedAt_Asc_ReturnsChronologicalOrder()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null, "createdAt", "asc"));

        var dates = result.Items.Select(d => d.CreatedAtUtc).ToList();
        Assert.Equal(dates.OrderBy(d => d).ToList(), dates);
    }

    [Fact]
    public async Task HandleAsync_SortByCreatedAt_Desc_ReturnsReverseChronologicalOrder()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var result = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null, "createdAt", "desc"));

        var dates = result.Items.Select(d => d.CreatedAtUtc).ToList();
        Assert.Equal(dates.OrderByDescending(d => d).ToList(), dates);
    }

    [Fact]
    public async Task HandleAsync_SortByNull_ReturnsCreatedAtAscOrder()
    {
        var handler = new GetDrawingListHandler(StubRepo.Build(), new GetDrawingListQueryValidator());

        var resultDefault = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null));
        var resultExplicit = await handler.HandleAsync(new GetDrawingListQuery(1, 10, null, null, "createdAt", "asc"));

        var defaultIds  = resultDefault.Items.Select(d => d.Id).ToList();
        var explicitIds = resultExplicit.Items.Select(d => d.Id).ToList();
        Assert.Equal(explicitIds, defaultIds);
    }
}
