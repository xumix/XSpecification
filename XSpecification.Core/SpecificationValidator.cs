using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Core;

/// <summary>
/// Shared specification validation helpers used by Linq / Elasticsearch backends.
/// Iterates every registered specification, instantiates a default filter and runs
/// the backend-specific projection delegate; aggregates any thrown exceptions.
/// </summary>
public static class SpecificationValidator
{
    /// <summary>
    /// Validates every <typeparamref name="TSpecification"/> registered in <paramref name="serviceProvider"/>
    /// by invoking <paramref name="projectFilter"/> with a default-constructed filter instance.
    /// Throws an <see cref="AggregateException"/> if at least one specification fails validation.
    /// </summary>
    /// <typeparam name="TSpecification">Backend-specific specification interface (e.g. <c>ISpecification</c>).</typeparam>
    /// <param name="serviceProvider">DI container.</param>
    /// <param name="projectFilter">Backend-specific projection invoked with the spec instance and a default filter.</param>
    public static void Validate<TSpecification>(
        IServiceProvider serviceProvider,
        Action<TSpecification, object> projectFilter)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(projectFilter);

        var specs = serviceProvider.GetServices<TSpecification>();
        var errors = new List<Exception>();

        foreach (var spec in specs)
        {
            if (spec is null)
            {
                continue;
            }

            try
            {
                // The closed core base type carries the (TModel, TFilter, TResult) tuple.
                var baseType = spec.GetType().GetClosedOfOpenGeneric(typeof(SpecificationBase<,,>));
                if (baseType == null)
                {
                    continue;
                }

                var filterType = baseType.GetGenericArguments()[1];
                var filterInstance = Activator.CreateInstance(filterType);
                if (filterInstance == null)
                {
                    continue;
                }

                projectFilter(spec, filterInstance);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
    }
}
