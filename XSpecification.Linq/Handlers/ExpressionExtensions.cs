using System.ComponentModel;
using System.Linq.Expressions;

namespace XSpecification.Linq.Handlers;

public static class ExpressionExtensions
{
    /// <summary>
    /// Creates a closure expression for the given <paramref name="value"/> so that the value is captured
    /// as a parameter (rather than embedded as a constant), which lets EF Core / SQL providers parametrize
    /// generated queries.
    /// </summary>
    /// <param name="value">The value to capture.</param>
    /// <param name="targetType">Target type the produced expression should evaluate to.</param>
    /// <remarks>Note: known not to work with <c>.Contains()</c> in EF Core 6.</remarks>
    public static Expression CreateClosure(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        Expression<Func<object?>> valueExpr = () => value;
        return valueExpr.Body.Type == targetType ? valueExpr.Body : Expression.Convert(valueExpr.Body, targetType);
    }

    /// <summary>
    /// Obsolete spelling of <see cref="CreateClosure"/>. Will be removed in a future major release.
    /// </summary>
    [Obsolete("Use CreateClosure instead. CreateClousre will be removed in a future major release.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Expression CreateClousre(object? value, Type targetType) => CreateClosure(value, targetType);
}
