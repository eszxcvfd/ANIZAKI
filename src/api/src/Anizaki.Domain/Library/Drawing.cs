using System;
using System.Collections.Generic;
using Anizaki.Domain.Abstractions;

namespace Anizaki.Domain.Library;

public sealed class Drawing : Entity<Guid>, IAggregateRoot
{
    private readonly List<string> _tags = new();

    public Drawing(
        Guid id,
        string title,
        string code,
        Guid categoryId,
        DrawingStatus status,
        DateTime createdAtUtc,
        DrawingFileInfo fileInfo) : base(id)
    {
        Title = title;
        Code = code;
        CategoryId = categoryId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        FileInfo = fileInfo;
    }

    public string Title { get; private set; }
    public string Code { get; private set; }
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public DrawingStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DrawingFileInfo FileInfo { get; private set; }
    public string? PreviewUrl { get; private set; }
    
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag))
        {
            _tags.Add(tag);
        }
    }

    public void SetPreviewUrl(string url)
    {
        PreviewUrl = url;
    }

    public void SetDescription(string description)
    {
        Description = description;
    }
}
