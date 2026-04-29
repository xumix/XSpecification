using System.Linq.Expressions;

using LinqKit;

namespace XSpecification.Linq;

/// <summary>
/// Composition helpers for LINQ specifications. They produce reusable
/// <see cref="Expression{TDelegate}"/> compositions of two specs that share the same
/// model and filter types.
/// </summary>
public static class SpecificationCompositionExtensions
{
    /// <summary>Combine two specifications with logical AND for a given filter instance.</summary>
    public static Expression<Func<TModel, bool>> And<TModel, TFilter>(
        this SpecificationBase<TModel, TFilter> left,
        SpecificationBase<TModel, TFilter> right,
        TFilter filter)
        where TFilter : class, new()
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(filter);

        return left.CreateFilterExpression(filter).And(right.CreateFilterExpression(filter));
    }

    /// <summary>Combine two specifications with logical OR for a given filter instance.</summary>
    public static Expression<Func<TModel, bool>> Or<TModel, TFilter>(
        this SpecificationBase<TModel, TFilter> left,
        SpecificationBase<TModel, TFilter> right,
        TFilter filter)
        where TFilter : class, new()
        where TModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(filter);

        return left.CreateFilterExpression(filter).Or(right.CreateFilterExpression(filter));
    }
}
