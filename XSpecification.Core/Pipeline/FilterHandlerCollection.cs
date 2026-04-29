using System.Collections;

namespace XSpecification.Core.Pipeline;

/// <summary>
/// Default <see cref="IFilterHandlerCollection"/> implementation. Encapsulates a
/// <see cref="LinkedList{T}"/> of handler types instead of inheriting from it (inheritance
/// from a mutable framework collection is an anti-pattern that leaks the entire surface).
/// </summary>
public abstract class FilterHandlerCollection : IFilterHandlerCollection
{
    private readonly LinkedList<Type> _handlers = new();

    /// <inheritdoc />
    public int Count => _handlers.Count;

    /// <inheritdoc />
    public void AddLast(Type filterHandler)
    {
        ArgumentNullException.ThrowIfNull(filterHandler);
        _handlers.AddLast(filterHandler);
    }

    /// <inheritdoc />
    public void AddFirst(Type filterHandler)
    {
        ArgumentNullException.ThrowIfNull(filterHandler);
        _handlers.AddFirst(filterHandler);
    }

    /// <inheritdoc />
    public void AddAfter<TFilter>(Type filterHandler)
    {
        ArgumentNullException.ThrowIfNull(filterHandler);

        var type = typeof(TFilter);
        var node = _handlers.Find(type)
            ?? throw new ArgumentException($"Unable to find filter {type} in pipeline.");
        _handlers.AddAfter(node, filterHandler);
    }

    /// <inheritdoc />
    public void AddBefore<TFilter>(Type filterHandler)
    {
        ArgumentNullException.ThrowIfNull(filterHandler);

        var type = typeof(TFilter);
        var node = _handlers.Find(type)
            ?? throw new ArgumentException($"Unable to find filter {type} in pipeline.");
        _handlers.AddBefore(node, filterHandler);
    }

    /// <inheritdoc />
    public void Clear() => _handlers.Clear();

    /// <inheritdoc />
    public bool Contains(Type filterHandler)
    {
        ArgumentNullException.ThrowIfNull(filterHandler);
        return _handlers.Contains(filterHandler);
    }

    /// <inheritdoc />
    public IEnumerable<Type> EnumerateReversed()
    {
        for (var node = _handlers.Last; node != null; node = node.Previous)
        {
            yield return node.Value;
        }
    }

    /// <inheritdoc />
    public IEnumerator<Type> GetEnumerator() => _handlers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
