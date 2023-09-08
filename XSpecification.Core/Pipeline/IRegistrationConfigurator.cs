using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Core.Pipeline;

public interface IRegistrationConfigurator
{
    IFilterHandlerCollection FilterHandlers { get; }
    IEnumerable<Type> Specifications { get; }
    IServiceCollection Services { get; }
    void AddSpecification<TSpecification>();
    void AddSpecifications(params Type[] specTypes);
    void Configure();
}
