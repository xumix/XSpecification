
namespace XSpecification.Linq;

public sealed class Options
{
    internal readonly LinkedList<Type> filterHandlers = new LinkedList<Type>();

    /// <summary>
    /// Disables convention-based automatic property handling
    /// </summary>
    public bool DisableAutoPropertyHandling { get; set; }

    public IList<Type> FilterHandlers => filterHandlers.ToArray();

    public void AddFilterHandler<THandler>()
    {
        AddFilterHandler(typeof(THandler));
    }

    public void AddFilterHandler(Type handlerType)
    {
        FilterHandlers.Insert(0, handlerType);
    }
}
