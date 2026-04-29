using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public class StringFilterHandler : IFilterHandler
{
    private static readonly FrozenDictionary<string, MethodInfo> TypeMethods =
        new Dictionary<string, MethodInfo>
        {
            {
                nameof(string.Contains), typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!
            },
            {
                nameof(string.StartsWith),
                typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!
            },
            { nameof(string.EndsWith), typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })! },
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private readonly ILogger<StringFilterHandler> _logger;

    public StringFilterHandler(ILogger<StringFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var ret = GetExpression(context);
        if (ret != default)
        {
            _logger.LogDebug("Created String expression: {Expression}", ret.Body);

            context.Expression.And(ret);
        }

        next(context);
    }

    public virtual bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!typeof(StringFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static Expression<Func<TModel, bool>>? GetExpression<TModel>(LinqFilterContext<TModel> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var propAccessor = context.ModelPropertyExpression!;
        var filter = (StringFilter)context.FilterPropertyValue!;

        if (!filter.HasValue())
        {
            return null;
        }

        var method = filter switch
        {
            { Contains: true } => TypeMethods[nameof(string.Contains)],
            { StartsWith: true } => TypeMethods[nameof(string.StartsWith)],
            { EndsWith: true } => TypeMethods[nameof(string.EndsWith)],
            _ => null
        };

        var value = ExpressionExtensions.CreateClosure(filter.Value, typeof(string));
        var memberBody = propAccessor.Body;

        Expression body;
        if (method == null)
        {
            body = filter switch
            {
                { IsNull: true } => Expression.Equal(memberBody, Expression.Constant(null)),
                { IsNotNull: true } => Expression.NotEqual(memberBody, Expression.Constant(null)),
                _ => Expression.Equal(memberBody, value)
            };
        }
        else
        {
            body = Expression.NotEqual(memberBody, Expression.Constant(null)); // null check
            body = Expression.AndAlso(body, Expression.Call(memberBody, method, value));
        }

        if (filter.IsInverted)
        {
            body = Expression.Not(body);
        }

        return (Expression<Func<TModel, bool>>)Expression.Lambda(body, propAccessor.Parameters);
    }
}
