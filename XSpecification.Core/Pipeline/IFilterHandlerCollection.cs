namespace XSpecification.Core.Pipeline;

/// <summary>
/// Ordered set of filter handler types describing a pipeline. Insertion order matches
/// pipeline execution order (first → last).
/// </summary>
public interface IFilterHandlerCollection : IEnumerable<Type>
{
    /// <summary>Number of registered handler types.</summary>
    int Count { get; }

    /// <summary>Append a handler type at the end of the pipeline.</summary>
    void AddLast(Type filterHandler);

    /// <summary>Prepend a handler type at the beginning of the pipeline.</summary>
    void AddFirst(Type filterHandler);

    /// <summary>Insert <paramref name="filterHandler"/> immediately after <typeparamref name="TFilter"/>.</summary>
    void AddAfter<TFilter>(Type filterHandler);

    /// <summary>Insert <paramref name="filterHandler"/> immediately before <typeparamref name="TFilter"/>.</summary>
    void AddBefore<TFilter>(Type filterHandler);

    /// <summary>Remove all registered handlers.</summary>
    void Clear();

    /// <summary>Returns <see langword="true"/> when the pipeline already contains <paramref name="filterHandler"/>.</summary>
    bool Contains(Type filterHandler);

    /// <summary>Iterate handlers in reverse order (last → first); used by pipeline builders.</summary>
    IEnumerable<Type> EnumerateReversed();
}
