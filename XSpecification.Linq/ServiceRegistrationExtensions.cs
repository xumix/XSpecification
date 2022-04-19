using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Linq;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddLinqSprecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<Options, IServiceProvider>? configure = null)
    {
        var builder = services.AddOptions<Options>();

        if (configure != null)
        {
            builder.Configure(configure);
        }

        return services;
    }

    public static IServiceCollection AddLinqSprecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<Options>? configure = null)
    {
        return AddLinqSprecification(services, (options, _) => configure?.Invoke(options));
    }

    public static IServiceCollection AddLinqSprecification(
        this IServiceCollection services)
    {
        return AddLinqSprecification(services, (Action<Options>?)null);
    }
}
