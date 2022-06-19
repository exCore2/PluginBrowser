namespace Model;

public class EquatableList<T> : List<T>, IEquatable<EquatableList<T>>
{
    public EquatableList()
    {
    }

    public EquatableList(IEnumerable<T> collection) : base(collection)
    {
    }

    public bool Equals(EquatableList<T>? other)
    {
        return other?.SequenceEqual(this) ?? false;
    }

    public override bool Equals(object? obj) => obj is EquatableList<T> other && Equals(other);
    public override int GetHashCode() => this.Aggregate(0, HashCode.Combine);
}

public static class EquatableListExtensions
{
    public static EquatableList<T> ToEquatableList<T>(this IEnumerable<T> source)
    {
        return new EquatableList<T>(source);
    }
}
