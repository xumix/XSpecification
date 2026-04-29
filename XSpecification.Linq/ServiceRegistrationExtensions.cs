using Microsoft.Extensions.DependencyInjection;

using XSpecification.Core;
using XSpecification.Core.Pipeline;
using XSpecification.Linq.Handlers;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Register the LINQ specification backend. Use the optional <paramref name="configure"/>
    /// callback to customize <see cref="SpecificationConfiguration"/>.
    /// </summary>
    public static IServiceCollection AddLinqSpecification(
        this IServiceCollection services,
        Action<IRegistrationConfigurator<LinqFilterHandlerCollection>> configureAction,
        Action<SpecificationConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureAction);

        var configuration = new SpecificationConfiguration();
        configure?.Invoke(configuration);
        services.AddSingleton(configuration);

        var configurator = new RegistrationConfigurator<ISpecification, LinqFilterHandlerCollection>(services);
        services.AddSingleton(typeof(IFilterHandlerPipeline<>), typeof(FilterHandlerPipeline<>));
        services.AddSingleton(configurator.FilterHandlers);

        configurator.FilterHandlers.AddLast(typeof(ConstantFilterHandler));
        configurator.FilterHandlers.AddLast(typeof(EnumerableFilterHandler));
        configurator.FilterHandlers.AddLast(typeof(NullableFilterHandler));
        configurator.FilterHandlers.AddLast(typeof(ListFilterHandler));
        configurator.FilterHandlers.AddLast(typeof(StringFilterHandler));
        configurator.FilterHandlers.AddLast(typeof(RangeFilterHandler));

        configureAction(configurator);
        configurator.Configure();

        return services;
    }

    /// <summary>
    /// Validates all registered LINQ specifications in the service provider by attempting
    /// to build a filter expression with a default filter instance.
    /// </summary>
    /// <param name="serviceProvider">The service provider containing the specifications to validate.</param>
    /// <exception cref="AggregateException">
    /// Thrown when one or more specifications fail validation. The inner exceptions contain
    /// details of each validation failure.
    /// </exception>
    public static void ValidateSpecifications(this IServiceProvider serviceProvider)
    {
        SpecificationValidator.Validate<ISpecification>(serviceProvider, (spec, filter) => spec.CreateFilterExpression(filter));
    }
}
