using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using XSpecification.Core;
using XSpecification.Linq.Handlers;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq;

public static class ServiceRegistrationExtensions
{
    public static OptionsBuilder<Options> AddLinqSpecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<IRegistrationConfigurator> configure)
    {

        var configurator = new RegistrationConfigurator();

        configurator.FilterHandlers.AddLast(typeof(ConstantFilterHandler));
        configurator.FilterHandlers.AddLast(typeof(NullableFilterHandler));

        configure(configurator);

        services.AddSingleton(typeof(IFilterHandlerPipeline<>), typeof(FilterHandlerPipeline<>));
        services.AddSingleton(configurator.FilterHandlers);

        foreach (var handler in configurator.FilterHandlers)
        {
            services.AddSingleton(handler);
        }

        foreach (var specification in configurator.Specifications)
        {
            services.AddTransient(typeof(ISpecification), specification);
            services.AddSingleton(specification);
        }

        return services.AddOptions<Options>();
    }

    public static void ValidateSpecifications(this IServiceProvider serviceProvider)
    {
        var specs = serviceProvider.GetServices<ISpecification>();

        var agg = new List<Exception>();

        foreach (var spec in specs)
        {
            try
            {
                var baseType = spec.GetType().GetClosedOfOpenGeneric(typeof(SpecificationBase<,>));
                if (baseType == null)
                {
                    continue;
                }

                var filterType = baseType.GetGenericArguments()[1];
                spec.CreateFilterExpression(Activator.CreateInstance(filterType)!);
            }
            catch (Exception e)
            {
                agg.Add(e);
            }
        }

        if (agg.Any())
        {
            throw new AggregateException(agg);
        }
    }
}

public class Options
{
    /// <summary>
    /// Disables convention-based property handling
    /// </summary>
    public bool DisablePropertyAutoHandling { get; set; }
}
