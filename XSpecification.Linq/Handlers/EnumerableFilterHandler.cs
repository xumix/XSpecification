using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class EnumerableFilterHandler : IFilterHandler
{
    private readonly ILogger<EnumerableFilterHandler> _logger;

    private static readonly IDictionary<string, MethodInfo> TypeMethods =
        new Dictionary<string, MethodInfo>
        {
            {
                nameof(Enumerable.Contains), typeof(Enumerable)
                                             .GetMethods()
                                             .First(m => m.Name == nameof(Enumerable.Contains) &&
                                                         m.GetParameters().Length == 2)
            }
        };

    public EnumerableFilterHandler(ILogger<EnumerableFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(Context<TModel> context, Action<Context<TModel>> next)
    {
        var ret = GetExpression(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Enumerable expression: {Expression}", ret.Body);

            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(Context<TModel> context)
    {
        var value = context.FilterPropertyValue!;
        return value is IEnumerable && value is not string && value is not IListFilter;
    }

    protected internal static Expression<Func<TModel, bool>> GetExpression<TModel>(Context<TModel> context)
    {
        var propAccessor = context.ModelPropertyExpression!;
        var propertyType = context.ModelProperty!.PropertyType;
        var enumerable = (IEnumerable)context.FilterPropertyValue!;

        // Check if the property type is the same as the filter type
        var castedValue = enumerable.ToArray(propertyType);
        var constant =
            ExpressionExtensions.CreateClousre(castedValue, typeof(IEnumerable<>).MakeGenericType(propertyType));

        var containsMethod = TypeMethods[nameof(Enumerable.Contains)];
        Expression body =
            Expression.Call(containsMethod.MakeGenericMethod(propertyType), constant, propAccessor.Body);

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
        return lam;
    }


}
