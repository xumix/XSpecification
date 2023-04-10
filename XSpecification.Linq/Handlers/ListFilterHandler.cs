using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class ListFilterHandler : IFilterHandler
{
    private readonly ILogger<ListFilterHandler> _logger;

    private static readonly IDictionary<string, MethodInfo> TypeMethods =
        new Dictionary<string, MethodInfo>
        {
            {
                nameof(Enumerable.Contains), typeof(Enumerable)
                                             .GetMethods()
                                             .First(m => m.Name == nameof(Enumerable.Contains) &&
                                                         m.GetParameters().Length == 2)
            },
            {
                nameof(Enumerable.Cast), typeof(Enumerable)
                                         .GetMethods()
                                         .First(m => m.Name == nameof(Enumerable.Cast) &&
                                                     m.GetParameters().Length == 1)
            }
        };

    public ListFilterHandler(ILogger<ListFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(Context<TModel> context, Action<Context<TModel>> next)
    {
        var ret = GetExpression(context);
        if (ret != default)
        {
            _logger.LogDebug("Created List expression: {Expression}", ret.Body);
            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(Context<TModel> context)
    {
        if (!typeof(IListFilter).IsAssignableFrom(context.FilterProperty.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static Expression<Func<TModel, bool>>? GetExpression<TModel>(Context<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var propertyType = context.ModelProperty!.PropertyType;
        var value = (IListFilter)context.FilterPropertyValue!;

        if (!value.HasValue())
        {
            return null;
        }

        var containsMethod = TypeMethods[nameof(Enumerable.Contains)];
        var constant = Expression.Constant(value.Values, typeof(IEnumerable<>).MakeGenericType(propertyType));

        Expression body =
            Expression.Call(containsMethod.MakeGenericMethod(propertyType), constant, propAccessor.Body);

        if (value.IsInverted)
        {
            body = Expression.Not(body);
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }
}
