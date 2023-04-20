using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq;

public abstract class SpecificationBase<TModel, TFilter> : ISpecification
    where TFilter : class, new()
    where TModel : class, new()
{
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

    private readonly ILogger<SpecificationBase<TModel, TFilter>> _logger;
    private readonly IFilterHandlerPipeline<TModel> _handlerPipeline;
    private readonly IOptions<Options> _options;

    protected SpecificationBase(
        ILogger<SpecificationBase<TModel, TFilter>> logger,
        IOptions<Options> options,
        IFilterHandlerPipeline<TModel> handlerPipeline)
    {
        _logger = logger;
        _handlerPipeline = handlerPipeline;
        _options = options;

        var unmatched = FilterProperties.Select(s => s.Name)
                                        .Where(f => !ModelProperties.ContainsKey(f));

        UnmatchedProps = new List<string>(unmatched);
        ExplicitHandlers = new Dictionary<string, FilterPropertyHandler>();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected internal IList<string> UnmatchedProps { get; }

    protected IDictionary<string, FilterPropertyHandler> ExplicitHandlers { get; }

    public virtual Expression<Func<TModel, bool>> CreateFilterExpression(TFilter filter)
    {
        var expressions = new List<Expression<Func<TModel, bool>>>();

        foreach (var filterProperty in FilterProperties)
        {
            if (ExplicitHandlers.ContainsKey(filterProperty.Name))
            {
                var handler = ExplicitHandlers[filterProperty.Name](filterProperty, filter);
                if (handler != null)
                {
                    expressions.Add(handler);
                }

                continue;
            }

            if (_options.Value.DisablePropertyAutoHandling)
            {
                continue;
            }

            var defHandler = CreateExpressionFromFilterProperty(filterProperty, filter);
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
            throw new AggregateException($"Unable add filter field handler: {filterProp}, property already handled", e);
        }

        var valueGetter = filterProp.Compile();
        ExplicitHandlers[member.Name] = (prop, filter) =>
        {
            var value = valueGetter(filter);
            return CreateExpressionFromFilterProperty(prop, entityProp, value);
        };
    }

    protected virtual void IgnoreField<TProp>(Expression<Func<TFilter, TProp>> filterProp)
    {
        HandleField(filterProp, (_, _) => null);
    }

    protected virtual Expression<Func<TModel, bool>>? CreateExpressionFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>>? modelProp,
        object? sourceValue)
    {
        if (modelProp == null || sourceValue == null || sourceValue as string == string.Empty)
        {
            return null;
        }

        try
        {
            var propertyFilterType = GetPropertyFilterType(filterProp, sourceValue);
            CheckTypeCompatibility(sourceValue, propertyFilterType);

            var context = new Context<TModel>
            {
                FilterProperty = filterProp,
                FilterPropertyValue = sourceValue,
                ModelProperty = (modelProp.Body as MemberExpression)!.Member as PropertyInfo,
                ModelPropertyExpression = modelProp
            };

            _handlerPipeline.Execute(context);

            return context.Expression;
        }
        catch (Exception e)
        {
            var format = "Unable to create filter {2} for property: {0}, model field: {1}";
            _logger.LogError(e, format, filterProp.Name, modelProp.Name, GetType());
            throw new AggregateException(string.Format(format, filterProp.Name, modelProp.Body, GetType().Name),
                e);
        }
    }

    protected delegate Expression<Func<TModel, bool>>? FilterPropertyHandler(
        PropertyInfo prop,
        TFilter filter);

    private static Type GetPropertyFilterType(PropertyInfo filterProp, object sourceValue)
    {
        Type filterPropType;
        if (sourceValue is IEnumerable
            && sourceValue is not string
            && sourceValue is not IFilter)
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

    private Expression<Func<TModel, bool>>? CreateExpressionFromFilterProperty(PropertyInfo filterProperty, object? filter)
    {
        var modelProperty = ModelProperties.ContainsKey(filterProperty.Name)
            ? ModelProperties[filterProperty.Name]
            : default;
        var modelPropertyExpression = modelProperty != null
            ? ModelPropertyExpressions[modelProperty.Name]
            : null;

        var sourceValue = filterProperty.GetValue(filter);

        // Do not filter on empty filter properties or unmatched model properties
        if (modelProperty == null || sourceValue == null
                                  || sourceValue as string == string.Empty)
        {
            return null;
        }

        var ret = ReflectionHelper.CallGenericMethod(this,
            nameof(CreateExpressionFromFilterProperty),
            modelProperty.PropertyType,
            new object?[] { filterProperty, modelPropertyExpression, sourceValue });

        return (Expression<Func<TModel, bool>>?)ret;
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
