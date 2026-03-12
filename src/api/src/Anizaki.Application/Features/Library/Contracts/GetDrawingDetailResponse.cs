using System;
using System.Collections.Generic;

namespace Anizaki.Application.Features.Library.Contracts;

public sealed record GetDrawingDetailResponse(
    Guid Id,
    string Title,
    string Code,
    string? Description,
    string CategorySlug,
    string CategoryName,
    string Status,
    IReadOnlyCollection<string> Tags,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    FileInfoDetailDto FileInfo);

public sealed record FileInfoDetailDto(
    string FileName,
    string MimeType,
    long SizeBytes,
    string Checksum,
    DateTime UploadedAtUtc,
    string PreviewAvailability);
