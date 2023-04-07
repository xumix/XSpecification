using System.Linq.Expressions;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class ConstantFilterHandler : IFilterHandler
{
    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(Context<TModel> context, Action<Context<TModel>> next)
    {
        var ret = GetExpression<TModel>(context.FilterProperty!.PropertyType,
            context.ModelPropertyExpression!,
            context.FilterPropertyValue);
        context.Expression.And(ret);
        next(context);
    }

    public virtual bool CanHandle<TModel>(Context<TModel> context)
    {
        return context.FilterPropertyValue is not IFilter;
    }

    protected internal static Expression<Func<TModel, bool>> GetExpression<TModel>(
        Type filterPropertyType,
        LambdaExpression propAccessor,
        object? value)
    {
        var body = Expression.Equal(propAccessor.Body, Expression.Constant(value, propAccessor.Body.Type));
        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }
}
