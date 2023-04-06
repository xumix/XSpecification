using System.Linq.Expressions;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class ConstantFilterHandler : IFilterHandler
{
    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(
        Context<TModel> context,
        Action<Context<TModel>> next)
    {
        var ret = GetConstantExpression(context);
        context.Expression.And(ret);
        next(context);
    }

    public virtual bool CanHandle<TModel>(Context<TModel> context)
    {
        return true;
    }

    protected static Expression<Func<TModel, bool>>? GetConstantExpression<TModel>(Context<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var value = context.FilterPropertyValue;
        var body = Expression.Equal(propAccessor.Body, Expression.Constant(value, context.FilterProperty!.PropertyType));
        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }
}
