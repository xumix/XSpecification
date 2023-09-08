using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class ListFilterHandler : IFilterHandler
{
    private readonly ILogger<ListFilterHandler> _logger;

    public ListFilterHandler(ILogger<ListFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        var ret = GetExpression(context);
        if (ret != default)
        {
            _logger.LogDebug("Created List expression: {Expression}", ret.Body);
            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        if (!typeof(IListFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static Expression<Func<TModel, bool>>? GetExpression<TModel>(LinqFilterContext<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var value = (IListFilter)context.FilterPropertyValue!;

        if (!value.HasValue())
        {
            return null;
        }

        var body = EnumerableFilterHandler.GetExpression<TModel>(context).Body;

        if (value.IsInverted)
        {
            body = Expression.Not(body);
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }
}
