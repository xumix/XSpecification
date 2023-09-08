using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Core.Pipeline;

public class RegistrationConfigurator<TSpec, TFilterCollection> : IRegistrationConfigurator
    where TFilterCollection : IFilterHandlerCollection, new()
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _specifications = new List<Type>();

    public RegistrationConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public IFilterHandlerCollection FilterHandlers { get; } = new TFilterCollection();

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
            _services.AddTransient(typeof(TSpec), specification);
            _services.AddSingleton(specification);
        }
    }
}
