using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using XSpecification.Core;
using XSpecification.Core.Pipeline;
using XSpecification.Linq.Handlers;
using XSpecification.Linq.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Linq;

public static class ServiceRegistrationExtensions
{
    public static OptionsBuilder<Options> AddLinqSpecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<IRegistrationConfigurator> configureAction)
    {

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
