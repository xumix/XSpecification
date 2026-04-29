using Nest;

namespace XSpecification.Elasticsearch;

/// <summary>
/// Composition helpers for Elasticsearch specifications. They produce reusable
/// <see cref="QueryContainer"/> compositions of two specs that share the same
/// model and filter types.
/// </summary>
public static class SpecificationCompositionExtensions
{
    /// <summary>Combine two specifications with logical AND for a given filter instance.</summary>
    public static QueryContainer And<TModel, TFilter>(
        this SpecificationBase<TModel, TFilter> left,
        SpecificationBase<TModel, TFilter> right,
        TFilter filter)
        where TFilter : class, new()
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(filter);

        return left.CreateFilterQuery(filter) && right.CreateFilterQuery(filter);
    }

    /// <summary>Combine two specifications with logical OR for a given filter instance.</summary>
    public static QueryContainer Or<TModel, TFilter>(
        this SpecificationBase<TModel, TFilter> left,
        SpecificationBase<TModel, TFilter> right,
        TFilter filter)
        where TFilter : class, new()
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(filter);

        return left.CreateFilterQuery(filter) || right.CreateFilterQuery(filter);
    }
}
