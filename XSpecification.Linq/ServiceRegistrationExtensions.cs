using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Linq;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddLinqSpecification(
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

    public static IServiceCollection AddLinqSpecification(
        this IServiceCollection services,
        // ReSharper disable once MethodOverloadWithOptionalParameter
        Action<Options>? configure = null)
    {
        return AddLinqSpecification(services, (options, _) => configure?.Invoke(options));
    }

    public static IServiceCollection AddLinqSpecification(
        this IServiceCollection services)
    {
        return AddLinqSpecification(services, (Action<Options>?)null);
    }
}
