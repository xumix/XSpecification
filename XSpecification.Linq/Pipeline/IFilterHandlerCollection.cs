namespace XSpecification.Linq.Pipeline;

public interface IFilterHandlerCollection : IEnumerable<Type>
{
    LinkedListNode<Type>? First { get; }
    LinkedListNode<Type>? Last { get; }
    void AddAfter<TFilter>(Type filterHandler);
    void AddBefore<TFilter>(Type filterHandler);
    LinkedListNode<Type> AddFirst(Type value);
    LinkedListNode<Type> AddLast(Type value);
    void Clear();
    bool Contains(Type value);
}
