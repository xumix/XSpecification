using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class NullableFilterHandler : IFilterHandler
{
    private readonly ILogger<NullableFilterHandler> _logger;

    public NullableFilterHandler(ILogger<NullableFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        var ret = GetExpression(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Nullable expression: {Expression}", ret.Body);
            context.Expression.And(ret);
        }
        else
        {
            next(context);
        }
    }

    public virtual bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        if (!typeof(INullableFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static Expression<Func<TModel, bool>>? GetExpression<TModel>(LinqFilterContext<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var propertyType = context.ModelProperty!.PropertyType;
        var value = (INullableFilter)context.FilterPropertyValue!;

        // if the property is not nullable, we can't filter on null
        if (!propertyType.IsNullable())
        {
            return value.IsNull ? KnownExpressions<TModel>.AlwaysFalseExpression : null;
        }

        var memberBody = propAccessor.Body;
        var body = value switch
        {
            { IsNull: true } => Expression.Equal(memberBody, Expression.Constant(null, propertyType)),
            { IsNotNull: true } => Expression.NotEqual(memberBody, Expression.Constant(null, propertyType)),
            _ => null
        };

        if (body == null)
        {
            return null;
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }
}
