using System;
using Anizaki.Domain.Abstractions;

namespace Anizaki.Domain.Library;

public sealed class DrawingFileInfo : ValueObject
{
    public DrawingFileInfo(
        string fileName,
        string mimeType,
        long sizeBytes,
        string checksum,
        DateTime uploadedAtUtc,
        PreviewAvailability previewAvailability)
    {
        FileName = fileName;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        Checksum = checksum;
        UploadedAtUtc = uploadedAtUtc;
        PreviewAvailability = previewAvailability;
    }

    public string FileName { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }
    public string Checksum { get; }
    public DateTime UploadedAtUtc { get; }
    public PreviewAvailability PreviewAvailability { get; }

    protected override System.Collections.Generic.IEnumerable<object> GetAtomicValues()
    {
        yield return FileName;
        yield return MimeType;
        yield return SizeBytes;
        yield return Checksum;
        yield return UploadedAtUtc;
        yield return PreviewAvailability;
    }
}
