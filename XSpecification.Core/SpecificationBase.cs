using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace XSpecification.Core;

public abstract class SpecificationBase<TModel, TFilter, TResult>
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

    private readonly bool _disablePropertyAutoHandling;

    protected SpecificationBase(bool disablePropertyAutoHandling)
    {
        _disablePropertyAutoHandling = disablePropertyAutoHandling;

        var unmatched = FilterProperties.Select(s => s.Name)
            .Where(f => !ModelProperties.ContainsKey(f));

        UnmatchedProps = new List<string>(unmatched);
        ExplicitHandlers = new Dictionary<string, FilterPropertyHandler>();
    }

    protected internal IList<string> UnmatchedProps { get; }

    protected IDictionary<string, FilterPropertyHandler> ExplicitHandlers { get; }

    protected virtual TResult CreateFilterResult(TFilter filter)
    {
        var results = new List<TResult>();

        foreach (var filterProperty in FilterProperties)
        {
            if (ExplicitHandlers.TryGetValue(filterProperty.Name, out var handler))
            {
                var result = handler(filterProperty, filter);
                if (result != null)
                {
                    results.Add(result);
                }

                continue;
            }

            if (_disablePropertyAutoHandling)
            {
                continue;
            }

            var defHandler = CreateResultFromFilterProperty(filterProperty, filter);
            if (defHandler != null)
            {
                results.Add(defHandler);
            }
        }

        CheckUnhandledProperties();

        return CombineResults(results);
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
            throw new AggregateException(
                $"Unable add filter field handler: {filterProp}, property already handled",
                e);
        }

        var valueGetter = filterProp.Compile();
        ExplicitHandlers[member.Name] = (prop, filter) =>
        {
            var value = valueGetter(filter);
            return CreateResultFromFilterProperty(prop, entityProp, value);
        };
    }

    protected virtual void IgnoreField<TProp>(Expression<Func<TFilter, TProp>> filterProp)
    {
        HandleField(filterProp, (_, _) => default);
    }

    protected virtual TResult? CreateResultFromFilterProperty(PropertyInfo filterProperty, object? filter)
    {
        ModelProperties.TryGetValue(filterProperty.Name, out var modelProperty);
        var modelPropertyExpression = modelProperty != null
            ? ModelPropertyExpressions[modelProperty.Name]
            : null;

        var sourceValue = filterProperty.GetValue(filter);

        if (modelProperty == null || sourceValue == null
            || (sourceValue is string sourceText && string.IsNullOrEmpty(sourceText)))
        {
            return default;
        }

        var ret = ReflectionHelper.CallGenericMethod(this,
            nameof(CreateResultFromFilterProperty),
            modelProperty.PropertyType,
            new object?[] { filterProperty, modelPropertyExpression, sourceValue });

        return (TResult?)ret;
    }

    protected abstract TResult CombineResults(IReadOnlyCollection<TResult> results);

    protected abstract TResult? CreateResultFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>>? modelProp,
        object? sourceValue);

    protected delegate TResult? FilterPropertyHandler(PropertyInfo prop, TFilter filter);

    protected static Type GetPropertyFilterType(PropertyInfo filterProp, object sourceValue)
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

    protected static void CheckTypeCompatibility(object sourceValue, Type propType)
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
}
