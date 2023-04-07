using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class EnumerableFilterHandler : IFilterHandler
{
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
                                                     m.GetParameters().Length == 2)
            }
        };

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
        var value = context.FilterPropertyValue!;
        return value is IEnumerable && !(value is string);
    }

    protected internal static Expression<Func<TModel, bool>>? GetExpression<TModel>(Context<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var propertyType = context.ModelProperty!.PropertyType;
        var enumerable = (IEnumerable)context.FilterPropertyValue!;

        var start = PredicateBuilder.New<TModel>(true);

        foreach (var val in enumerable)
        {
            var elExp = ConstantFilterHandler.GetExpression<TModel>(propertyType, propAccessor, val);
            if (elExp == null)
            {
                continue;
            }

            start = start.Or(elExp);
        }

        return start.IsStarted ? start : null;
    }


}
