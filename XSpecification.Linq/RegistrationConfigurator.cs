
using Microsoft.Extensions.DependencyInjection;

using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq;

public interface IRegistrationConfigurator
{
    IFilterHandlerCollection FilterHandlers { get; }
    IEnumerable<Type> Specifications { get; }
    IServiceCollection Services { get; }
    void AddSpecification<TSpecification>();
    void AddSpecifications(params Type[] specTypes);
    void Configure();
}

internal sealed class RegistrationConfigurator : IRegistrationConfigurator
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _specifications = new List<Type>();

    public RegistrationConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public IFilterHandlerCollection FilterHandlers { get; } = new FilterHandlerCollection();

    public IEnumerable<Type> Specifications => _specifications.ToArray();

    public IServiceCollection Services => _services;

    public void AddSpecification<TSpecification>()
    {
        AddSpecifications(typeof(TSpecification));
    }

    public void AddSpecifications(params Type[] specTypes)
    {
        _specifications.AddRange(specTypes);
    }

    public void Configure()
    {
        foreach (var handler in FilterHandlers)
        {
            _services.AddSingleton(handler);
        }

        foreach (var specification in Specifications)
        {
            _services.AddTransient(typeof(ISpecification), specification);
            _services.AddSingleton(specification);
        }
    }
}
