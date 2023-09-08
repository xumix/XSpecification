using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class RangeFilterHandler : IFilterHandler
{
    private readonly ILogger<RangeFilterHandler> _logger;

    public RangeFilterHandler(ILogger<RangeFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        var ret = GetExpression(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Range expression: {Expression}", ret.Body);
            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        if (!typeof(IRangeFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected internal Expression<Func<TModel, bool>>? GetExpression<TModel>(LinqFilterContext<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var rangeFilter = (IRangeFilter)context.FilterPropertyValue!;

        if (!rangeFilter.HasValue())
        {
            return null;
        }

        if (rangeFilter.UseStartAsEquals)
        {
            return ConstantFilterHandler.GetExpression<TModel>(context.FilterProperty!.PropertyType,
                propAccessor,
                rangeFilter.Start);
        }

        var param = Expression.Parameter(typeof(TModel));
        var memberBody = new ParameterVisitor(propAccessor.Parameters, new[] { param }).Visit(propAccessor.Body)!;

        var elementType = rangeFilter.ElementType;

        if (memberBody.Type.IsNullable())
        {
            elementType = typeof(Nullable<>).MakeGenericType(elementType);
        }

        var start = () => ExpressionExtensions.CreateClousre(rangeFilter.Start, elementType);
        var end = () => ExpressionExtensions.CreateClousre(rangeFilter.End, elementType);

        var more = () => rangeFilter.IsExclusive
            ? Expression.GreaterThan(memberBody, start())
            : Expression.GreaterThanOrEqual(memberBody, start());

        var less = () => rangeFilter.IsExclusive
            ? Expression.LessThan(memberBody, end())
            : Expression.LessThanOrEqual(memberBody, end());

        Expression? body = (rangeFilter.Start != null, rangeFilter.End != null) switch
        {
            (true, true) => Expression.AndAlso(more(), less()),
            (true, _) => more(),
            (_, true) => less(),
            _ => null
        };

        if (body == null)
        {
            return null;
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, param);
        return lam;
    }
}
