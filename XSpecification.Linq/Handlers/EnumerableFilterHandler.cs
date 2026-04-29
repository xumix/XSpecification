using System.Collections;
using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Core.Pipeline;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class EnumerableFilterHandler : IFilterHandler
{
    private static readonly FrozenDictionary<string, MethodInfo> TypeMethods =
        new Dictionary<string, MethodInfo>
        {
            {
                nameof(Enumerable.Contains), typeof(Enumerable)
                                             .GetMethods()
                                             .First(m => m.Name == nameof(Enumerable.Contains) &&
                                                         m.GetParameters().Length == 2)
            },
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private readonly ILogger<EnumerableFilterHandler> _logger;

    public EnumerableFilterHandler(ILogger<EnumerableFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var ret = GetExpression<TModel>(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Enumerable expression: {Expression}", ret.Body);

            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var value = context.FilterPropertyValue!;
        return value is IEnumerable && value is not string && value is not IListFilter;
    }

    protected internal static Expression<Func<TModel, bool>> GetExpression<TModel>(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var propAccessor = context.ModelPropertyExpression!;
        var propertyType = context.ModelProperty!.PropertyType;
        var enumerable = (IEnumerable)context.FilterPropertyValue!;

        // Check if the property type is the same as the filter type
        var castedValue = enumerable.ToArray(propertyType);
        var constant =
            ExpressionExtensions.CreateClosure(castedValue, typeof(IEnumerable<>).MakeGenericType(propertyType));

        var containsMethod = TypeMethods[nameof(Enumerable.Contains)];
        Expression body =
            Expression.Call(containsMethod.MakeGenericMethod(propertyType), constant, propAccessor.Body);

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }


}
