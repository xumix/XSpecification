using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace XSpecification.Core;

[RequiresUnreferencedCode("Specifications use reflection over TFilter/TModel properties; AOT trimming may remove members.")]
[RequiresDynamicCode("Specifications use MakeGenericMethod and Expression.Compile() which are not AOT-friendly.")]
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

    // Cache of compiled per-(model-property-type) thunks that dispatch into the
    // generic abstract CreateResultFromFilterProperty<TProperty> (virtual call).
    // This replaces ReflectionHelper.CallGenericMethod on the hot path.
    private static readonly ConcurrentDictionary<Type, GenericDispatcher> CreateResultDispatcherCache = new();

    private static readonly MethodInfo OpenGenericCreateResultMethod = typeof(SpecificationBase<TModel, TFilter, TResult>)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        .Single(m => m.Name == nameof(CreateResultFromFilterProperty)
                     && m.IsGenericMethodDefinition);

    private readonly bool _disablePropertyAutoHandling;
    private readonly List<string> _unmatchedProps;
    private readonly Dictionary<string, FilterPropertyHandler> _explicitHandlers;

    // Toggled on the first CreateFilterResult call to short-circuit subsequent
    // CheckUnhandledProperties() invocations on the hot path.
    private bool _unhandledPropertiesChecked;

    protected SpecificationBase(bool disablePropertyAutoHandling)
    {
        _disablePropertyAutoHandling = disablePropertyAutoHandling;

        var unmatched = FilterProperties.Select(s => s.Name)
            .Where(f => !ModelProperties.ContainsKey(f));

        _unmatchedProps = new List<string>(unmatched);
        _explicitHandlers = new Dictionary<string, FilterPropertyHandler>();
    }

    /// <summary>
    /// Filter property names that are not yet mapped to a model property — either by convention
    /// (same name on <typeparamref name="TModel"/>) or via an explicit <c>HandleField</c> /
    /// <c>IgnoreField</c> registration. The collection becomes empty once all filter properties
    /// are covered.
    /// </summary>
    protected IReadOnlyCollection<string> UnhandledFilterProperties => _unmatchedProps;

    /// <summary>Read-only view of explicit handlers registered via <c>HandleField</c>.</summary>
    protected IReadOnlyDictionary<string, FilterPropertyHandler> ExplicitHandlers => _explicitHandlers;

    /// <summary>
    /// Legacy mutable accessor preserved for 1.x source compatibility — internal to allow
    /// existing test fixtures to assert on it without exposing mutability publicly.
    /// </summary>
    [Obsolete("Use UnhandledFilterProperties (IReadOnlyCollection<string>). This member will become private in 3.0.")]
    protected internal IList<string> UnmatchedProps => _unmatchedProps;

    protected virtual TResult CreateFilterResult(TFilter filter)
    {
        if (!_unhandledPropertiesChecked)
        {
            // Validate spec configuration once on first use; not on every call.
            CheckUnhandledProperties();
            _unhandledPropertiesChecked = true;
        }

        var results = new List<TResult>();

        foreach (var filterProperty in FilterProperties)
        {
            if (_explicitHandlers.TryGetValue(filterProperty.Name, out var handler))
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

        return CombineResults(results);
    }

    protected virtual void HandleField<TProp>(
        Expression<Func<TFilter, TProp>> filterProp,
        FilterPropertyHandler handler)
    {
        ArgumentNullException.ThrowIfNull(filterProp);
        ArgumentNullException.ThrowIfNull(handler);

        var member = ReflectionHelper.GetPropertyInfo(filterProp);
        if (!_unmatchedProps.Remove(member.Name) && _explicitHandlers.ContainsKey(member.Name))
        {
            throw new InvalidOperationException(
                $"Unable add filter field handler: {filterProp}, property already handled");
        }

        _explicitHandlers[member.Name] = handler;

        // Re-validate on next CreateFilterResult call.
        _unhandledPropertiesChecked = false;
    }

    protected virtual void HandleField<TFilterProp, TModelProp>(
        Expression<Func<TFilter, TFilterProp>> filterProp,
        Expression<Func<TModel, TModelProp>> entityProp)
    {
        ArgumentNullException.ThrowIfNull(filterProp);
        ArgumentNullException.ThrowIfNull(entityProp);

        var member = ReflectionHelper.GetPropertyInfo(filterProp);
        if (!_unmatchedProps.Remove(member.Name) && _explicitHandlers.ContainsKey(member.Name))
        {
            throw new InvalidOperationException(
                $"Unable add filter field handler: {filterProp}, property already handled");
        }

        var valueGetter = filterProp.Compile();
        _explicitHandlers[member.Name] = (prop, filter) =>
        {
            var value = valueGetter(filter);
            return CreateResultFromFilterProperty(prop, entityProp, value);
        };

        _unhandledPropertiesChecked = false;
    }

    protected virtual void IgnoreField<TProp>(Expression<Func<TFilter, TProp>> filterProp)
    {
        ArgumentNullException.ThrowIfNull(filterProp);
        HandleField(filterProp, (_, _) => default);
    }

    protected virtual TResult? CreateResultFromFilterProperty(PropertyInfo filterProperty, object? filter)
    {
        ArgumentNullException.ThrowIfNull(filterProperty);

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

        var dispatcher = CreateResultDispatcherCache.GetOrAdd(modelProperty.PropertyType, BuildDispatcher);
        return dispatcher(this, filterProperty, modelPropertyExpression, sourceValue);
    }

    protected abstract TResult CombineResults(IReadOnlyCollection<TResult> results);

    protected abstract TResult? CreateResultFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>>? modelProp,
        object? sourceValue);

    protected delegate TResult? FilterPropertyHandler(PropertyInfo prop, TFilter filter);

    protected static Type GetPropertyFilterType(PropertyInfo filterProp, object sourceValue)
    {
        ArgumentNullException.ThrowIfNull(filterProp);
        ArgumentNullException.ThrowIfNull(sourceValue);

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
        ArgumentNullException.ThrowIfNull(sourceValue);
        ArgumentNullException.ThrowIfNull(propType);

        if (!propType.IsInstanceOfType(sourceValue)
            && !(sourceValue is IEnumerable && propType.GetGenericElementType().IsInstanceOfType(sourceValue)))
        {
            throw new InvalidOperationException(string.Format(
                "Filter property type: '{0}' does not match with source value type: '{1}'",
                propType.Name,
                sourceValue.GetType().Name));
        }
    }

    private static GenericDispatcher BuildDispatcher(Type modelPropertyType)
    {
        var closedMethod = OpenGenericCreateResultMethod.MakeGenericMethod(modelPropertyType);

        var instanceParam = Expression.Parameter(typeof(SpecificationBase<TModel, TFilter, TResult>), "instance");
        var filterPropParam = Expression.Parameter(typeof(PropertyInfo), "filterProp");
        var modelPropExprParam = Expression.Parameter(typeof(LambdaExpression), "modelPropExpr");
        var sourceValueParam = Expression.Parameter(typeof(object), "sourceValue");

        // Cast LambdaExpression -> Expression<Func<TModel, TProperty>> (typed)
        var typedExpression = typeof(Expression<>)
            .MakeGenericType(typeof(Func<,>).MakeGenericType(typeof(TModel), modelPropertyType));

        var castedExpr = Expression.TypeAs(modelPropExprParam, typedExpression);

        var call = Expression.Call(instanceParam, closedMethod, filterPropParam, castedExpr, sourceValueParam);

        var lambda = Expression.Lambda<GenericDispatcher>(
            call,
            instanceParam,
            filterPropParam,
            modelPropExprParam,
            sourceValueParam);

        return lambda.Compile();
    }

    private void CheckUnhandledProperties()
    {
        if (_unmatchedProps.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Filter '{typeof(TFilter)}' properties " +
            $"'{string.Join(",", _unmatchedProps)}'" +
            $" are not mapped to entity '{typeof(TModel)}' fields");
    }

    private delegate TResult? GenericDispatcher(
        SpecificationBase<TModel, TFilter, TResult> instance,
        PropertyInfo filterProperty,
        LambdaExpression? modelPropertyExpression,
        object? sourceValue);
}
