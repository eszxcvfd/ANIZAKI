namespace Anizaki.Domain.Abstractions;

public abstract record DomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid EventId { get; }

    public DateTime OccurredOnUtc { get; }
}

