
namespace XSpecification.Linq;

public sealed class Options
{
    internal readonly List<Type> filterHandlers = new List<Type>();

    internal readonly List<Type> specifications = new List<Type>();

    /// <summary>
    /// Disables convention-based property handling
    /// </summary>
    public bool DisablePropertyAutoHandling { get; set; }

    public IList<Type> FilterHandlers => filterHandlers.ToArray();

    public IEnumerable<Type> Specifications => specifications.ToArray();

    public void AddFilterHandler<THandler>()
    {
        AddFilterHandler(typeof(THandler));
    }

    public void AddFilterHandler(Type handlerType)
    {
        filterHandlers.Insert(0, handlerType);
    }

    public void AddSpecification<TSpecification>()
    {
        AddSpecification(typeof(TSpecification));
    }

    public void AddSpecification(params Type[] specTypes)
    {
        specifications.AddRange(specTypes);
    }
}
