using System.Collections;

namespace XSpecification.Linq.Pipeline;

/// <summary>
/// Enumerator for a pipeline builder.
/// </summary>
internal sealed class FilterHandlerCollectionEnumerator : IEnumerator, IEnumerator<Type>
{
    private readonly LinkedListNode<Type>? _first;
    private LinkedListNode<Type> _current;
    private bool _ended;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterHandlerCollectionEnumerator"/> class.
    /// </summary>
    /// <param name="first">The first middleware declaration.</param>
    public FilterHandlerCollectionEnumerator(LinkedListNode<Type>? first)
    {
        _first = first;
    }

    /// <inheritdoc />
    object IEnumerator.Current => _current?.Value ?? throw new InvalidOperationException();

    /// <inheritdoc />
    public Type? Current => _current?.Value ?? throw new InvalidOperationException();

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_ended)
        {
            return false;
        }

        if (_current != null)
        {
            _current = _current.Next;

            _ended = !(_current != null);

            return !_ended;
        }

        if (_first != null)
        {
            _current = _first;

            return true;
        }

        _ended = true;
        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _current = null;
        _ended = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose here.
    }
}
