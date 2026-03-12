using System;
using Anizaki.Domain.Abstractions;

namespace Anizaki.Domain.Library;

public sealed class DrawingCategory : Entity<Guid>, IAggregateRoot
{
    public DrawingCategory(Guid id, string name, string slug, int order) : base(id)
    {
        Name = name;
        Slug = slug;
        Order = order;
    }

    public string Name { get; private set; }
    public string Slug { get; private set; }
    public int Order { get; private set; }
}
