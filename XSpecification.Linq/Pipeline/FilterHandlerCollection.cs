using System.Collections;

namespace XSpecification.Linq.Pipeline;

internal class FilterHandlerCollection : LinkedList<Type>, IFilterHandlerCollection
{
    public void AddAfter<TFilter>()
    {
        var type = typeof(TFilter);
        var node = Find(type);
        if (node == null)
        {
            throw new ArgumentException($"Unable to find filter {type} in pipeline.");
        }

        AddAfter(node, type);
    }

    public void AddBefore<TFilter>()
    {
        var type = typeof(TFilter);
        var node = Find(type);
        if (node == null)
        {
            throw new ArgumentException($"Unable to find filter {type} in pipeline.");
        }

        AddBefore(node, type);
    }

    /// <inheritdoc/>
    public new IEnumerator<Type> GetEnumerator()
    {
        return new FilterHandlerCollectionEnumerator(First);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
