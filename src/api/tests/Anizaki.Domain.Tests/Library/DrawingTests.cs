using System;
using Anizaki.Domain.Library;
using Xunit;

namespace Anizaki.Domain.Tests.Library;

public class DrawingTests
{
    private static DrawingFileInfo CreateValidFileInfo()
        => new("test.dwg", "application/acad", 1024, "sha256:abc", DateTime.UtcNow, PreviewAvailability.Unavailable);

    [Fact]
    public void Constructor_Initialization_SetsPropertiesCorrectly()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = DateTime.UtcNow;
        var info = CreateValidFileInfo();

        var drawing = new Drawing(id, "Cốt Thép Sàn", "STR-100", categoryId, DrawingStatus.Published, date, info);

        Assert.Equal(id, drawing.Id);
        Assert.Equal("Cốt Thép Sàn", drawing.Title);
        Assert.Equal("STR-100", drawing.Code);
        Assert.Equal(categoryId, drawing.CategoryId);
        Assert.Equal(DrawingStatus.Published, drawing.Status);
        Assert.Equal(date, drawing.CreatedAtUtc);
        Assert.Same(info, drawing.FileInfo);
        Assert.Empty(drawing.Tags);
        Assert.Null(drawing.PreviewUrl);
        Assert.Null(drawing.Description);
    }

    [Fact]
    public void AddTag_ValidTag_AddsToCollection()
    {
        var drawing = new Drawing(Guid.NewGuid(), "Title", "Code", Guid.NewGuid(), DrawingStatus.Draft, DateTime.UtcNow, CreateValidFileInfo());
        
        drawing.AddTag("kien-truc");
        drawing.AddTag("noi-that");

        Assert.Equal(2, drawing.Tags.Count);
        Assert.Contains("kien-truc", drawing.Tags);
        Assert.Contains("noi-that", drawing.Tags);
    }

    [Fact]
    public void AddTag_NullOrWhitespace_IgnoresTag()
    {
        var drawing = new Drawing(Guid.NewGuid(), "Title", "Code", Guid.NewGuid(), DrawingStatus.Draft, DateTime.UtcNow, CreateValidFileInfo());
        
        drawing.AddTag("");
        drawing.AddTag("   ");
        drawing.AddTag(null!);

        Assert.Empty(drawing.Tags);
    }

    [Fact]
    public void AddTag_DuplicateTag_IgnoresTag()
    {
        var drawing = new Drawing(Guid.NewGuid(), "Title", "Code", Guid.NewGuid(), DrawingStatus.Draft, DateTime.UtcNow, CreateValidFileInfo());
        
        drawing.AddTag("kien-truc");
        drawing.AddTag("kien-truc");

        Assert.Single(drawing.Tags);
    }

    [Fact]
    public void SetPreviewUrl_UpdatesProperty()
    {
        var drawing = new Drawing(Guid.NewGuid(), "Title", "Code", Guid.NewGuid(), DrawingStatus.Draft, DateTime.UtcNow, CreateValidFileInfo());
        
        drawing.SetPreviewUrl("https://ex.com/preview.png");

        Assert.Equal("https://ex.com/preview.png", drawing.PreviewUrl);
    }

    [Fact]
    public void SetDescription_UpdatesProperty()
    {
        var drawing = new Drawing(Guid.NewGuid(), "Title", "Code", Guid.NewGuid(), DrawingStatus.Draft, DateTime.UtcNow, CreateValidFileInfo());
        
        drawing.SetDescription("Bản vẽ kiến trúc");

        Assert.Equal("Bản vẽ kiến trúc", drawing.Description);
    }
}

public class DrawingFileInfoTests
{
    [Fact]
    public void Constructor_Initialization_SetsPropertiesCorrectly()
    {
        var d = DateTime.UtcNow;
        var info = new DrawingFileInfo("arc.pdf", "application/pdf", 5000, "sha256:123", d, PreviewAvailability.Available);

        Assert.Equal("arc.pdf", info.FileName);
        Assert.Equal("application/pdf", info.MimeType);
        Assert.Equal(5000, info.SizeBytes);
        Assert.Equal("sha256:123", info.Checksum);
        Assert.Equal(d, info.UploadedAtUtc);
        Assert.Equal(PreviewAvailability.Available, info.PreviewAvailability);
    }

    [Fact]
    public void ValueObjects_WithSameValues_AreEqual()
    {
        var d = DateTime.UtcNow;
        var info1 = new DrawingFileInfo("arc.pdf", "application/pdf", 5000, "sha256:123", d, PreviewAvailability.Available);
        var info2 = new DrawingFileInfo("arc.pdf", "application/pdf", 5000, "sha256:123", d, PreviewAvailability.Available);
        var info3 = new DrawingFileInfo("arc.pdf", "application/pdf", 5001, "sha256:123", d, PreviewAvailability.Available);

        Assert.Equal(info1, info2);
        Assert.True(info1 == info2);
        Assert.NotEqual(info1, info3);
        Assert.True(info1 != info3);
    }
}
