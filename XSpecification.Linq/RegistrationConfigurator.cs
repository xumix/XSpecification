
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq;

public interface IRegistrationConfigurator
{
    IFilterHandlerCollection FilterHandlers { get; }
    IEnumerable<Type> Specifications { get; }
    void AddSpecification<TSpecification>();
    void AddSpecifications(params Type[] specTypes);
}

internal sealed class RegistrationConfigurator : IRegistrationConfigurator
{
    private readonly List<Type> _specifications = new List<Type>();

    public IFilterHandlerCollection FilterHandlers { get; } = new FilterHandlerCollection();

    public IEnumerable<Type> Specifications => _specifications.ToArray();

    public void AddSpecification<TSpecification>()
    {
        AddSpecifications(typeof(TSpecification));
    }

    public void AddSpecifications(params Type[] specTypes)
    {
        _specifications.AddRange(specTypes);
    }
}
