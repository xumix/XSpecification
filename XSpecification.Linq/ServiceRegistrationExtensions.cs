using Microsoft.Extensions.DependencyInjection;

using XSpecification.Core;

namespace XSpecification.Linq;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddLinqSpecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<Options> configure)
    {
        var builder = services.AddOptions<Options>();
        var options = new Options();

        //options.AddFilterHandler<>();

        configure(options);

        foreach (var specification in options.Specifications)
        {
            services.AddTransient(typeof(ISpecification), specification);
            services.AddSingleton(specification);
        }

        return services;
    }

    // public static IServiceCollection AddLinqSpecification(
    //     this IServiceCollection services,
    //     // ReSharper disable once MethodOverloadWithOptionalParameter
    //     Action<Options>? configure = null)
    // {
    //     return AddLinqSpecification(services, (options, _) =>
    //     {
    //         configure?.Invoke(options);
    //         foreach (var specification in options.Specifications)
    //         {
    //             services.AddScoped(typeof(ISpecification), specification);
    //             services.AddSingleton(specification);
    //         }
    //     });
    // }

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
