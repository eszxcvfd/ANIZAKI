using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anizaki.Application.Features.Library.Contracts;

namespace Anizaki.Infrastructure.Library;

/// <summary>
/// In-memory library repository seeded with deterministic fixture data per
/// docs/library-seeded-dataset.md. Used as a read-only baseline until
/// a persistent data layer is introduced.
/// </summary>
public sealed class InMemoryLibraryRepository : ILibraryRepository
{
    // ─── Category Fixture ────────────────────────────────────────────────────

    private static readonly Guid CatKienTrucId  = new("01000000-0000-0000-0000-000000000001");
    private static readonly Guid CatKetCauId    = new("01000000-0000-0000-0000-000000000002");
    private static readonly Guid CatCoDienId    = new("01000000-0000-0000-0000-000000000003");
    private static readonly Guid CatNoiThatId   = new("01000000-0000-0000-0000-000000000004");

    // ─── Drawing Fixture ─────────────────────────────────────────────────────

    private static readonly Guid DrawingId1 = new("d1000000-0000-0000-0000-000000000001");
    private static readonly Guid DrawingId2 = new("d1000000-0000-0000-0000-000000000002");
    private static readonly Guid DrawingId3 = new("d1000000-0000-0000-0000-000000000003");
    private static readonly Guid DrawingId4 = new("d1000000-0000-0000-0000-000000000004");
    private static readonly Guid DrawingId5 = new("d1000000-0000-0000-0000-000000000005");
    private static readonly Guid DrawingId6 = new("d1000000-0000-0000-0000-000000000006");

    private sealed record DrawingRecord(
        Guid Id,
        string Title,
        string Code,
        string? Description,
        Guid CategoryId,
        string CategorySlug,
        string CategoryName,
        string Status,
        DateTime CreatedAtUtc,
        string FileName,
        string MimeType,
        long SizeBytes,
        string Checksum,
        DateTime UploadedAtUtc,
        string PreviewAvailability,
        string? PreviewType,
        string? PreviewUrl,
        string? PreviewMessage,
        IReadOnlyCollection<string> Tags);

    private static readonly IReadOnlyList<DrawingRecord> Drawings = new[]
    {
        // Drawing 1: image preview available
        new DrawingRecord(
            Id: DrawingId1,
            Title: "Mặt Bằng Tầng Trệt",
            Code: "ARC-001",
            Description: "Bản vẽ mặt bằng tầng trệt của công trình Nhà ở Hà Nội.",
            CategoryId: CatKienTrucId,
            CategorySlug: "kien-truc",
            CategoryName: "Kiến Trúc",
            Status: "published",
            CreatedAtUtc: new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            FileName: "floorplan.png",
            MimeType: "image/png",
            SizeBytes: 204800,
            Checksum: "sha256:abc123def456",
            UploadedAtUtc: new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            PreviewAvailability: "available",
            PreviewType: "image",
            PreviewUrl: "/mock/floorplan.png",
            PreviewMessage: null,
            Tags: ["mặt bằng", "tầng trệt", "nhà ở"]),

        // Drawing 2: PDF preview available
        new DrawingRecord(
            Id: DrawingId2,
            Title: "Mặt Cắt Đứng Trục A-D",
            Code: "ARC-002",
            Description: "Mặt cắt đứng theo trục A-D thể hiện chi tiết cấu tạo tầng.",
            CategoryId: CatKienTrucId,
            CategorySlug: "kien-truc",
            CategoryName: "Kiến Trúc",
            Status: "published",
            CreatedAtUtc: new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc),
            FileName: "section.pdf",
            MimeType: "application/pdf",
            SizeBytes: 512000,
            Checksum: "sha256:def789abc012",
            UploadedAtUtc: new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc),
            PreviewAvailability: "available",
            PreviewType: "pdf",
            PreviewUrl: "/mock/section.pdf",
            PreviewMessage: null,
            Tags: ["mặt cắt", "kết cấu"]),

        // Drawing 3: CAD file — preview unavailable
        new DrawingRecord(
            Id: DrawingId3,
            Title: "Bản Vẽ Chi Tiết Mái",
            Code: "ARC-003",
            Description: null,
            CategoryId: CatKienTrucId,
            CategorySlug: "kien-truc",
            CategoryName: "Kiến Trúc",
            Status: "published",
            CreatedAtUtc: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            FileName: "roof-detail.dwg",
            MimeType: "application/acad",
            SizeBytes: 1048576,
            Checksum: "sha256:cad001moo002",
            UploadedAtUtc: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            PreviewAvailability: "unavailable",
            PreviewType: null,
            PreviewUrl: null,
            PreviewMessage: "Định dạng CAD (.dwg) không hỗ trợ xem trước trực tiếp.",
            Tags: ["mái", "chi tiết"]),

        // Drawing 4: PDF — preview still generating
        new DrawingRecord(
            Id: DrawingId4,
            Title: "Cốt Thép Sàn Tầng 2",
            Code: "STR-001",
            Description: "Bản vẽ bố trí cốt thép sàn tầng 2 công trình văn phòng.",
            CategoryId: CatKetCauId,
            CategorySlug: "ket-cau",
            CategoryName: "Kết Cấu",
            Status: "published",
            CreatedAtUtc: new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            FileName: "rebar-t2.pdf",
            MimeType: "application/pdf",
            SizeBytes: 768000,
            Checksum: "sha256:rebar001xx99",
            UploadedAtUtc: new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            PreviewAvailability: "generating",
            PreviewType: null,
            PreviewUrl: null,
            PreviewMessage: "Bản xem trước đang được tạo. Vui lòng thử lại sau ít phút.",
            Tags: ["cốt thép", "sàn"]),

        // Drawing 5: RAR archive — preview unavailable
        new DrawingRecord(
            Id: DrawingId5,
            Title: "Khung Thép Chịu Lực",
            Code: "STR-002",
            Description: "Bộ bản vẽ khung thép chịu lực nén.",
            CategoryId: CatKetCauId,
            CategorySlug: "ket-cau",
            CategoryName: "Kết Cấu",
            Status: "published",
            CreatedAtUtc: new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            FileName: "frame.rar",
            MimeType: "application/x-rar-compressed",
            SizeBytes: 2097152,
            Checksum: "sha256:rar001framex",
            UploadedAtUtc: new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            PreviewAvailability: "unavailable",
            PreviewType: null,
            PreviewUrl: null,
            PreviewMessage: "Định dạng nén (.rar) không hỗ trợ xem trước trực tiếp.",
            Tags: ["khung thép", "chịu lực"]),

        // Drawing 6: Draft status — PDF preview available
        new DrawingRecord(
            Id: DrawingId6,
            Title: "Hệ Thống Điện Chiếu Sáng",
            Code: "MEP-001",
            Description: "Thiết kế sơ bộ hệ thống chiếu sáng nội thất văn phòng.",
            CategoryId: CatCoDienId,
            CategorySlug: "co-dien",
            CategoryName: "Cơ Điện",
            Status: "draft",
            CreatedAtUtc: new DateTime(2026, 2, 10, 14, 0, 0, DateTimeKind.Utc),
            FileName: "lighting.pdf",
            MimeType: "application/pdf",
            SizeBytes: 315000,
            Checksum: "sha256:light001pdff",
            UploadedAtUtc: new DateTime(2026, 2, 10, 14, 0, 0, DateTimeKind.Utc),
            PreviewAvailability: "available",
            PreviewType: "pdf",
            PreviewUrl: "/mock/lighting.pdf",
            PreviewMessage: null,
            Tags: ["điện", "chiếu sáng", "nội thất"]),
    };

    private static readonly IReadOnlyList<CategoryItemDto> Categories;

    static InMemoryLibraryRepository()
    {
        // Compute drawingCount from fixture data
        var counts = Drawings.GroupBy(d => d.CategoryId)
                             .ToDictionary(g => g.Key, g => g.Count());

        Categories = new[]
        {
            new CategoryItemDto(CatKienTrucId, "Kiến Trúc", "kien-truc", 1, counts.GetValueOrDefault(CatKienTrucId, 0)),
            new CategoryItemDto(CatKetCauId,   "Kết Cấu",   "ket-cau",   2, counts.GetValueOrDefault(CatKetCauId,   0)),
            new CategoryItemDto(CatCoDienId,   "Cơ Điện",   "co-dien",   3, counts.GetValueOrDefault(CatCoDienId,   0)),
            new CategoryItemDto(CatNoiThatId,  "Nội Thất",  "noi-that",  4, counts.GetValueOrDefault(CatNoiThatId,  0)),
        };
    }

    // ─── ILibraryRepository ───────────────────────────────────────────────────

    public Task<IReadOnlyCollection<CategoryItemDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CategoryItemDto>>(Categories);
    }

    public Task<(IReadOnlyCollection<DrawingItemDto> Items, int TotalItems)> GetDrawingsAsync(
        int page,
        int pageSize,
        string? categorySlug,
        string? search,
        string? sortBy,
        string? sortDir,
        CancellationToken cancellationToken)
    {
        var query = Drawings.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(d => d.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.Code.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = (sortBy switch
        {
            "title"     => desc ? query.OrderByDescending(d => d.Title)        : query.OrderBy(d => d.Title),
            "code"      => desc ? query.OrderByDescending(d => d.Code)         : query.OrderBy(d => d.Code),
            "createdAt" => desc ? query.OrderByDescending(d => d.CreatedAtUtc) : query.OrderBy(d => d.CreatedAtUtc),
            _           => query.OrderBy(d => d.CreatedAtUtc),
        }).ToList();
        var totalItems = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DrawingItemDto(
                d.Id,
                d.Title,
                d.Code,
                d.CategorySlug,
                d.CategoryName,
                d.Status,
                d.CreatedAtUtc,
                d.PreviewUrl))
            .ToList();

        return Task.FromResult<(IReadOnlyCollection<DrawingItemDto>, int)>((items, totalItems));
    }

    public Task<GetDrawingDetailResponse?> GetDrawingDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var d = Drawings.FirstOrDefault(x => x.Id == id);
        if (d is null) return Task.FromResult<GetDrawingDetailResponse?>(null);

        var response = new GetDrawingDetailResponse(
            d.Id,
            d.Title,
            d.Code,
            d.Description,
            d.CategorySlug,
            d.CategoryName,
            d.Status,
            d.Tags,
            d.CreatedAtUtc,
            UpdatedAtUtc: null,
            new FileInfoDetailDto(
                d.FileName,
                d.MimeType,
                d.SizeBytes,
                d.Checksum,
                d.UploadedAtUtc,
                d.PreviewAvailability));

        return Task.FromResult<GetDrawingDetailResponse?>(response);
    }

    public Task<GetDrawingPreviewResponse?> GetDrawingPreviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var d = Drawings.FirstOrDefault(x => x.Id == id);
        if (d is null) return Task.FromResult<GetDrawingPreviewResponse?>(null);

        var response = new GetDrawingPreviewResponse(
            d.Id,
            d.PreviewAvailability,
            d.PreviewType,
            d.PreviewUrl,
            d.PreviewMessage);

        return Task.FromResult<GetDrawingPreviewResponse?>(response);
    }
}
