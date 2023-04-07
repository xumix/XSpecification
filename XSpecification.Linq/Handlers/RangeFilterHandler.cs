using System.Linq.Expressions;
using System.Reflection;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class RangeFilterHandler : IFilterHandler
{
    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(Context<TModel> context, Action<Context<TModel>> next)
    {
        var ret = GetExpression(context);
        if (ret != default)
        {
            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(Context<TModel> context)
    {
        if (!typeof(IRangeFilter).IsAssignableFrom(context.FilterProperty.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static Expression<Func<TModel, bool>>? GetExpression<TModel>(Context<TModel> context)
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

        var constant = Expression.Constant(rangeFilter);
        var start = Expression.Property(constant, nameof(IRangeFilter.Start));
        var end = Expression.Property(constant, nameof(IRangeFilter.End));

        if (!memberBody.Type.IsNullable()
            && start.Type.IsNullable())
        {
            memberBody = Expression.Convert(memberBody, start.Type);
        }

        var more = rangeFilter.IsExclusive
            ? Expression.GreaterThan(memberBody, start)
            : Expression.GreaterThanOrEqual(memberBody, start);

        var less = rangeFilter.IsExclusive
            ? Expression.LessThan(memberBody, end)
            : Expression.LessThanOrEqual(memberBody, end);

        Expression? body = (rangeFilter.Start != null, rangeFilter.End != null) switch
        {
            (true, true) => Expression.AndAlso(more, less),
            (true, _) => more,
            (_, true) => less,
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
