using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class ConstantFilterHandler : IFilterHandler
{
    private readonly ILogger<ConstantFilterHandler> _logger;

    public ConstantFilterHandler(ILogger<ConstantFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(Context<TModel> context, Action<Context<TModel>> next)
    {
        var ret = GetExpression<TModel>(context.FilterProperty!.PropertyType,
            context.ModelPropertyExpression!,
            context.FilterPropertyValue);
        context.Expression.And(ret);

        _logger.LogDebug("Created Constant expression: {Expression}", ret.Body);

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
        var valueExpr = ExpressionExtensions.CreateClousre(value, propAccessor.Body.Type);

        var body = Expression.Equal(propAccessor.Body, valueExpr);
        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }
}
