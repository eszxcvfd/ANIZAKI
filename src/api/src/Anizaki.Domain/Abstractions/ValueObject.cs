namespace Anizaki.Domain.Abstractions;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetAtomicValues();

    public bool Equals(ValueObject? other)
    {
        if (other is null || GetType() != other.GetType())
        {
            return false;
        }

        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (var value in GetAtomicValues())
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}

