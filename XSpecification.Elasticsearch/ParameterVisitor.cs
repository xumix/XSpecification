using System.Linq.Expressions;

namespace XSpecification.Elasticsearch;

internal class ParameterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression[] _from;
    private readonly ParameterExpression[] _to;

    public ParameterVisitor(IEnumerable<ParameterExpression> from, IEnumerable<ParameterExpression> to)
    {
        if (@from == null)
        {
            throw new ArgumentNullException("from");
        }

        if (to == null)
        {
            throw new ArgumentNullException("to");
        }

        if (@from.Count() != to.Count())
        {
            throw new InvalidOperationException("Parameter lengths must match");
        }

        _from = @from.ToArray();
        _to = to.ToArray();
    }

    public static LambdaExpression Merge(LambdaExpression fromExpression, LambdaExpression toExpression, Func<Expression, Expression, Expression> mergeFunc)
    {
        var newBody = new ParameterVisitor(toExpression.Parameters, fromExpression.Parameters).VisitAndConvert(toExpression.Body, "Merge");
        return Expression.Lambda(mergeFunc(fromExpression.Body, newBody), fromExpression.Parameters);
    }

    public static Expression<T> Merge<T>(Expression<T> fromExpression, Expression<T> toExpression, Func<Expression, Expression, Expression> mergeFunc)
    {
        return (Expression<T>)Merge((LambdaExpression)fromExpression, toExpression, mergeFunc);
    }

    public static LambdaExpression AndAlso(LambdaExpression fromExpression, LambdaExpression toExpression)
    {
        var newBody = new ParameterVisitor(toExpression.Parameters, fromExpression.Parameters).VisitAndConvert(toExpression.Body, "AndAlso");
        return Expression.Lambda(Expression.AndAlso(fromExpression.Body, newBody), fromExpression.Parameters);
    }

    public static Expression<T> AndAlso<T>(Expression<T> fromExpression, Expression<T> toExpression)
    {
        return (Expression<T>)AndAlso((LambdaExpression)fromExpression, toExpression);
    }

    public static LambdaExpression OrElse(LambdaExpression fromExpression, LambdaExpression toExpression)
    {
        var newBody = new ParameterVisitor(toExpression.Parameters, fromExpression.Parameters).VisitAndConvert(toExpression.Body, "OrElse");
        return Expression.Lambda(Expression.OrElse(fromExpression.Body, newBody), fromExpression.Parameters);
    }

    public static Expression<T> OrElse<T>(Expression<T> fromExpression, Expression<T> toExpression)
    {
        return (Expression<T>)OrElse((LambdaExpression)fromExpression, toExpression);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        for (var i = 0; i < _from.Length; i++)
        {
            if (node == _from[i])
            {
                return _to[i];
            }
        }

        return node;
    }
}
