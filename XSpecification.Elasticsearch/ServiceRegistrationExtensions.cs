using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using XSpecification.Core;
using XSpecification.Core.Pipeline;
using XSpecification.Elasticsearch.Handlers;
using XSpecification.Elasticsearch.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Elasticsearch;

public static class ServiceRegistrationExtensions
{
    public static OptionsBuilder<Options> AddElasticSpecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<IRegistrationConfigurator<ElasticFilterHandlerCollection>> configureAction)
    {

        var configurator = new RegistrationConfigurator<ISpecification, ElasticFilterHandlerCollection>(services);
        services.AddSingleton(typeof(IFilterHandlerPipeline), typeof(FilterHandlerPipeline));
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
                spec.CreateFilterQuery(Activator.CreateInstance(filterType)!);
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
