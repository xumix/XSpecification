using System.Linq.Expressions;

namespace XSpecification.Linq.Handlers;

public static class ExpressionExtensions
{
    /// <summary>
    /// Creates a clousre expression for the given value to parametrize SQL query.
    /// Does not work with .Contains() method as of EF Core 6.
    /// </summary>
    public static Expression CreateClousre(object? value, Type targetType)
    {
        Expression<Func<object?>> valueExpr = () => value;
        return Expression.Convert(valueExpr.Body, targetType);
    }
}
