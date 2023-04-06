namespace XSpecification.Linq.Pipeline;

public interface IFilterHandlerCollection : IEnumerable<Type>
{
    LinkedListNode<Type>? First { get; }
    LinkedListNode<Type>? Last { get; }
    void AddAfter<TFilter>();
    void AddBefore<TFilter>();
    LinkedListNode<Type> AddFirst(Type value);
    LinkedListNode<Type> AddLast(Type value);
    void Clear();
    bool Contains(Type value);
}
