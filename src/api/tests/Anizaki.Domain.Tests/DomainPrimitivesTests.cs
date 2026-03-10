using Anizaki.Domain.Abstractions;
using Anizaki.Domain.Exceptions;
using Anizaki.Domain.ValueObjects;

namespace Anizaki.Domain.Tests;

public class DomainPrimitivesTests
{
    [Fact]
    public void Entity_WithSameTypeAndId_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var left = new SampleEntity(id);
        var right = new SampleEntity(id);

        Assert.Equal(left, right);
        Assert.True(left == right);
    }

    [Fact]
    public void ValueObject_WithSameAtomicValues_ShouldBeEqual()
    {
        var left = new Money(2500, "USD");
        var right = new Money(2500, "USD");

        Assert.Equal(left, right);
        Assert.True(left == right);
    }

    [Fact]
    public void DomainEvent_ShouldCaptureEventIdAndTimestamp()
    {
        var before = DateTime.UtcNow;
        var @event = new SampleDomainEvent();
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.InRange(@event.OccurredOnUtc, before, after);
    }

    [Fact]
    public void DomainException_ShouldExposeMessage()
    {
        const string message = "Domain rule violated";
        var exception = new DomainException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ServiceHealth_ShouldNormalizeExternalStatus()
    {
        var value = ServiceHealth.FromExternal("HEALTHY");

        Assert.Equal("healthy", value.Value);
    }

    [Fact]
    public void ServiceHealth_ShouldRejectUnknownStatus()
    {
        Assert.Throws<DomainException>(() => ServiceHealth.FromExternal("unknown"));
    }

    private sealed class SampleEntity : Entity<Guid>, IAggregateRoot
    {
        public SampleEntity(Guid id)
            : base(id)
        {
        }
    }

    private sealed class Money : ValueObject
    {
        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }

        public string Currency { get; }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed record SampleDomainEvent : DomainEvent;
}
