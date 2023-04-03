using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Core;

namespace XSpecification.Linq;

public abstract class SpecificationBase<TModel, TFilter> : ISpecification
    where TFilter : class, new()
    where TModel : class, new()
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IDictionary<Type, IDictionary<string, MethodInfo>> TypeMethods =
        new Dictionary<Type, IDictionary<string, MethodInfo>>
        {
            {
                typeof(Enumerable),
                new Dictionary<string, MethodInfo>
                {
                    {
                        nameof(Enumerable.Contains),
                        typeof(Enumerable).GetMethods()
                                          .First(m => m.Name == nameof(Enumerable.Contains)
                                                      && m.GetParameters().Length == 2)
                    }
                }
            },
            {
                typeof(string),
                new Dictionary<string, MethodInfo>
                {
                    {
                        nameof(string.Contains),
                        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!
                    },
                    {
                        nameof(string.StartsWith),
                        typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!
                    },
                    {
                        nameof(string.EndsWith),
                        typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!
                    },
                }
            },
        };

    private static readonly PropertyInfo[] FilterProperties = typeof(TFilter).GetProperties();

    private static readonly Dictionary<string, PropertyInfo> ModelProperties = typeof(TModel)
                                                                               .GetProperties()
                                                                               .ToDictionary(s => s.Name);

    private static readonly Dictionary<string, LambdaExpression> ModelPropertyExpressions =
        ModelProperties.ToDictionary(k => k.Key, v =>
        {
            var param = Expression.Parameter(typeof(TModel));
            var body = Expression.MakeMemberAccess(param, v.Value);
            var lam = Expression.Lambda(body, param);

            return lam;
        });

    private readonly ILogger<SpecificationBase<TModel, TFilter>> logger;
    private readonly IOptions<Options> options;

    protected SpecificationBase(
        ILogger<SpecificationBase<TModel, TFilter>> logger,
        IOptions<Options> options)
    {
        this.logger = logger;
        this.options = options;

        var unmatched = FilterProperties.Select(s => s.Name)
                                        .Where(f => !ModelProperties.ContainsKey(f));

        UnmatchedProps = new List<string>(unmatched);
        ExplicitHandlers = new Dictionary<string, FilterPropertyHandler>();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected IList<string> UnmatchedProps { get; }

    protected IDictionary<string, FilterPropertyHandler> ExplicitHandlers { get; }

    public virtual Expression<Func<TModel, bool>> CreateFilterExpression(TFilter filter)
    {
        var expressions = new List<Expression<Func<TModel, bool>>>();

        foreach (var filterProperty in FilterProperties)
        {
            var context = new ExpressionCreationContext<TModel>();

            context.FilterProperty = filterProperty;
            context.FilterPropertyValue = filterProperty.GetValue(filter);
            context.ModelProperty = ModelProperties.ContainsKey(filterProperty.Name)
                ? ModelProperties[filterProperty.Name]
                : default;
            context.ModelPropertyExpression = context.ModelProperty != null
                ? ModelPropertyExpressions[context.ModelProperty.Name]
                : null;

            if (ExplicitHandlers.ContainsKey(filterProperty.Name))
            {
                var handler = ExplicitHandlers[filterProperty.Name](filterProperty, filter);
                if (handler != null)
                {
                    expressions.Add(handler);
                }

                continue;
            }

            if (options.Value.DisablePropertyAutoHandling)
            {
                continue;
            }

            var defHandler = CreateExpressionFromFilterProperty(context);
            // Do not spoil the result with no-ops
            if (defHandler != null)
            {
                expressions.Add(defHandler);
            }
        }

        CheckUnhandledProperties();

        var result = PredicateBuilder.New<TModel>(true);
        foreach (var expression in expressions)
        {
            result.And(expression);
        }

        var optimized = ExpressionOptimizer.tryVisit(result);
        return (Expression<Func<TModel, bool>>)optimized;
    }

    protected static Expression<Func<TModel, bool>>? GetListExpression<TProperty>(
        Expression<Func<TModel, TProperty>> prop,
        IListFilter listFilter)
    {
        if (!listFilter.HasValue())
        {
            return null;
        }

        var containsMethod = TypeMethods[typeof(Enumerable)][nameof(Enumerable.Contains)];
        var constant = Expression.Constant(listFilter.Cast<TProperty>().ToArray());

        Expression body =
            Expression.Call(containsMethod.MakeGenericMethod(typeof(TProperty)), constant, prop.Body);

        if (listFilter.IsInverted)
        {
            body = Expression.Not(body);
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
        return lam;
    }

    protected static Expression<Func<TModel, bool>>? GetStringExpression<TProperty>(
        Expression<Func<TModel, TProperty>> prop,
        StringFilter? filter)
    {
        return GetStringExpression<TModel, TProperty>(prop, filter);
    }

    protected static Expression<Func<TM, bool>>? GetStringExpression<TM, TProperty>(
        Expression<Func<TM, TProperty>> prop,
        StringFilter? filter)
    {
        if (filter == null || !filter.HasValue())
        {
            return null;
        }

        var method = filter switch
        {
            { Contains: true } => TypeMethods[typeof(string)][nameof(string.Contains)],
            { StartsWith: true } => TypeMethods[typeof(string)][nameof(string.StartsWith)],
            { EndsWith: true } => TypeMethods[typeof(string)][nameof(string.EndsWith)],
            _ => null
        };

        var constant = Expression.Constant(filter);
        var value = Expression.MakeMemberAccess(constant, filter.GetMemberInfo(f => f.Value));
        var memberBody = prop.Body;

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

        return (Expression<Func<TM, bool>>)Expression.Lambda(body, prop.Parameters);
    }

    protected static Expression<Func<TModel, bool>>? GetNullableExpression<TProperty>(
        Expression<Func<TModel, TProperty>> prop,
        INullableFilter value)
    {
        if (!typeof(TProperty).IsNullable())
        {
            return value.IsNull ? KnownExpressions<TModel>.AlwaysFalseExpression : null;
        }

        var memberBody = prop.Body;
        var body = value switch
        {
            { IsNull: true } => Expression.Equal(memberBody, Expression.Constant(null, typeof(TProperty))),
            { IsNotNull: true } => Expression.NotEqual(memberBody, Expression.Constant(null, typeof(TProperty))),
            _ => null
        };

        if (body == null)
        {
            return null;
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
        return lam;
    }

    protected static Expression<Func<TModel, bool>>? GetConstantExpression<TProperty>(
        Expression<Func<TModel, TProperty>> prop,
        object? value)
    {
        var body = Expression.Equal(prop.Body, Expression.Constant(value, typeof(TProperty)));
        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
        return lam;
    }

    protected static Expression<Func<TModel, bool>>? GetRangeExpression<TProperty>(
        Expression<Func<TModel, TProperty>> prop,
        IRangeFilter rangeFilter)
    {
        if (rangeFilter.UseStartAsEquals)
        {
            return GetConstantExpression(prop, rangeFilter.Start);
        }

        var param = Expression.Parameter(typeof(TModel));
        var memberBody = new ParameterVisitor(prop.Parameters, new[] { param }).Visit(prop.Body);

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

    protected virtual void HandleField<TProp>(
        Expression<Func<TFilter, TProp>> filterProp,
        FilterPropertyHandler handler)
    {
        var member = ReflectionHelper.GetPropertyInfo(filterProp);
        if (!UnmatchedProps.Remove(member.Name))
        {
            throw new InvalidOperationException(
                $"Unable add filter field handler: {filterProp}, property already handled");
        }

        ExplicitHandlers[member.Name] = handler;
    }

    protected virtual void HandleField<TFilterProp, TModelProp>(
        Expression<Func<TFilter, TFilterProp>> filterProp,
        Expression<Func<TModel, TModelProp>> entityProp)
    {
        var member = ReflectionHelper.GetPropertyInfo(filterProp);
        try
        {
            UnmatchedProps.Remove(member.Name);
        }
        catch (Exception e)
        {
            throw new AggregateException($"Unable add filter field handler: {filterProp}, property already handled",
                e);
        }

        var valueGetter = filterProp.Compile();
        ExplicitHandlers[member.Name] = (prop, filter) =>
        {
            var value = valueGetter(filter);
            return CreateExpressionFromFilterProperty(prop, entityProp, value);
        };
    }

    protected virtual Expression<Func<TModel, bool>>? CreateExpressionFromFilterProperty(
        ExpressionCreationContext<TModel> context)
    {
        if (context.ModelProperty == null)
        {
            return null;
        }

        var modelPropertyExpression = ModelPropertyExpressions[context.ModelProperty.Name];

        var ret = ReflectionHelper.CallGenericMethod(this,
            nameof(CreateExpressionFromFilterProperty),
            context.ModelProperty.PropertyType,
            new object?[] { context.FilterProperty, modelPropertyExpression, context.FilterPropertyValue });

        return (Expression<Func<TModel, bool>>)ret!;
    }

    protected virtual Expression<Func<TModel, bool>>? CreateExpressionFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>> modelProp,
        object? sourceValue)
    {
        if (sourceValue == null || sourceValue as string == string.Empty)
        {
            return null;
        }

        var propertyFilterType = GetPropertyFilterType(filterProp, sourceValue);

        try
        {
            CheckTypeCompatibility(sourceValue, propertyFilterType);

            if (typeof(INullableFilter).IsAssignableFrom(propertyFilterType))
            {
                var nullableFilter = (INullableFilter)sourceValue;
                var ret = GetNullableExpression(modelProp, nullableFilter);
                if (ret != null)
                {
                    return ret;
                }
            }

            if (typeof(IListFilter).IsAssignableFrom(propertyFilterType))
            {
                var listFilter = (IListFilter)sourceValue;
                return GetListExpression(modelProp, listFilter);
            }

            if (typeof(StringFilter).IsAssignableFrom(propertyFilterType))
            {
                var stringFilter = (StringFilter)sourceValue;
                return GetStringExpression<TModel, TProperty>(modelProp, stringFilter);
            }

            if (typeof(IRangeFilter).IsAssignableFrom(propertyFilterType))
            {
                var rangeFilter = (IRangeFilter)sourceValue;
                return GetRangeExpression(modelProp, rangeFilter);
            }

            if (sourceValue is IEnumerable enumerable && !(sourceValue is string))
            {
                var start = PredicateBuilder.New<TModel>(true);

                foreach (var val in enumerable)
                {
                    start = start.Or(CreateExpressionFromFilterProperty(filterProp, modelProp, val));
                }

                return start.IsStarted ? start : null;
            }

            return GetConstantExpression(modelProp, sourceValue);
        }
        catch (Exception e)
        {
            var format = "Unable to create filter {2} for property: {0}, model field: {1}";
            logger.LogError(format, filterProp.Name, modelProp.Name, GetType());
            throw new AggregateException(string.Format(format, filterProp.Name, modelProp.Body, GetType().Name),
                e);
        }
    }

    protected void IgnoreField<TProp>(Expression<Func<TFilter, TProp>> filterProp)
    {
        HandleField(filterProp, (_, _) => null);
    }

    protected delegate Expression<Func<TModel, bool>>? FilterPropertyHandler(
        PropertyInfo prop,
        TFilter filter);

    private static Type GetPropertyFilterType(PropertyInfo filterProp, object sourceValue)
    {
        Type filterPropType;
        if (sourceValue is IEnumerable
            && sourceValue is not string
            && sourceValue is not INullableFilter)
        {
            var elementType = filterProp.PropertyType.GetGenericElementType();
            filterPropType = elementType;
        }
        else
        {
            filterPropType = filterProp.PropertyType;
        }

        return filterPropType;
    }

    private static void CheckTypeCompatibility(object sourceValue, Type propType)
    {
        if (!propType.IsInstanceOfType(sourceValue)
            && !(sourceValue is IEnumerable && propType.GetGenericElementType().IsInstanceOfType(sourceValue)))
        {
            throw new InvalidOperationException(string.Format(
                "Filter property type: '{0}' does not match with source value type: '{1}'",
                propType.Name,
                sourceValue.GetType().Name));
        }
    }

    private void CheckUnhandledProperties()
    {
        if (!UnmatchedProps.Any())
        {
            return;
        }

        throw new InvalidOperationException(
            $"Filter '{typeof(TFilter)}' properties " +
            $"'{string.Join(",", UnmatchedProps.Select(s => s.ToString()))}'" +
            $" are not mapped to entity '{typeof(TModel)}' fields");
    }

    /// <inheritdoc />
    LambdaExpression ISpecification.CreateFilterExpression(object filter)
    {
        return CreateFilterExpression((TFilter)filter);
    }
}
