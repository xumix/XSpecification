using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Nest;

using Newtonsoft.Json.Serialization;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elasticsearch;

public abstract class SpecificationBase<TModel, TFilter>
    : XSpecification.Core.SpecificationBase<TModel, TFilter, QueryContainer>,
        ISpecification
    where TFilter : class, new()
    where TModel : class, new()
{
    public static readonly NamingStrategy NamingStrategy = new CamelCaseNamingStrategy();

    private readonly ILogger<SpecificationBase<TModel, TFilter>> _logger;
    private readonly IFilterHandlerPipeline _handlerPipeline;

    protected SpecificationBase(
        ILogger<SpecificationBase<TModel, TFilter>> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline handlerPipeline)
        : base((configuration ?? throw new ArgumentNullException(nameof(configuration))).DisablePropertyAutoHandling)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(handlerPipeline);

        _logger = logger;
        _handlerPipeline = handlerPipeline;
    }

    /// <summary>
    /// Backward-compatible constructor that accepts the legacy <see cref="Options"/> wrapped in
    /// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>. Prefer the
    /// <see cref="SpecificationConfiguration"/> overload.
    /// </summary>
    [Obsolete("Use the SpecificationConfiguration overload. This constructor will be removed in 3.0.")]
    protected SpecificationBase(
        ILogger<SpecificationBase<TModel, TFilter>> logger,
        Microsoft.Extensions.Options.IOptions<Options> options,
        IFilterHandlerPipeline handlerPipeline)
        : this(
            logger,
            (options ?? throw new ArgumentNullException(nameof(options))).Value.ToConfiguration(),
            handlerPipeline)
    {
    }

    public virtual QueryContainer CreateFilterQuery(TFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return CreateFilterResult(filter);
    }

    /// <summary>
    /// Asynchronous variant of <see cref="CreateFilterQuery"/>. The current Elasticsearch backend
    /// builds queries synchronously; the async signature is provided as a forward-compatible
    /// contract (e.g. for future I/O-bound visitors) and respects <paramref name="cancellationToken"/>.
    /// </summary>
    public virtual ValueTask<QueryContainer> CreateFilterQueryAsync(
        TFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(CreateFilterQuery(filter));
    }

    protected virtual QueryContainer? CreateQueryFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>>? modelProp,
        object? sourceValue)
    {
        return CreateResultFromFilterProperty(filterProp, modelProp, sourceValue);
    }

    protected override QueryContainer CombineResults(IReadOnlyCollection<QueryContainer> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var result = results
            .Where(x => x != null)
            .OrderBy(x => (x as IQueryContainer)?.Bool?.Filter?.Any() ?? false)
            .Aggregate(new QueryContainer(), (acc, x) => acc && x);

        return result;
    }

    protected override QueryContainer? CreateResultFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>>? modelProp,
        object? sourceValue)
    {
        ArgumentNullException.ThrowIfNull(filterProp);

        if (modelProp == null || sourceValue == null
            || (sourceValue is string sourceText && string.IsNullOrEmpty(sourceText)))
        {
            return null;
        }

        try
        {
            var propertyFilterType = GetPropertyFilterType(filterProp, sourceValue);
            CheckTypeCompatibility(sourceValue, propertyFilterType);

            var context = new QueryContext
            {
                FilterProperty = filterProp,
                FilterPropertyValue = sourceValue,
                ModelProperty = (modelProp.Body as MemberExpression)!.Member as PropertyInfo,
                ModelPropertyExpression = modelProp,
                IndexFieldName = QueryHelpers.GetPropertyPath(modelProp, NamingStrategy)
            };

            _handlerPipeline.Execute(context);

            return context.QueryContainer;
        }
        catch (Exception e)
        {
            var format = "Unable to create filter {2} for property: {0}, model field: {1}";
            _logger.LogError(e, format, filterProp.Name, modelProp.Name, GetType());
            throw new AggregateException(string.Format(format, filterProp.Name, modelProp.Body, GetType().Name),
                e);
        }
    }

    protected virtual string GetIndexFieldName<TProp>(Expression<Func<TModel, TProp>> indexPath)
    {
        return GetIndexFieldName<TModel, TProp>(indexPath);
    }

    protected virtual string GetIndexFieldName<T, TProp>(Expression<Func<T, TProp>> indexPath)
    {
        return QueryHelpers.GetPropertyPath(indexPath, NamingStrategy);
    }

    /// <summary>
    /// Register a filter property whose value is matched against any of the supplied model
    /// properties (logical OR). Replaces the manual pattern of calling
    /// <c>CreateQueryFromFilterProperty</c> for each model property and combining the results
    /// with <c>||</c>.
    /// </summary>
    protected void OrGroup<TFilterProp>(
        Expression<Func<TFilter, TFilterProp>> filterProp,
        params Expression<Func<TModel, TFilterProp>>[] modelProps)
    {
        ArgumentNullException.ThrowIfNull(filterProp);
        ArgumentNullException.ThrowIfNull(modelProps);
        if (modelProps.Length == 0)
        {
            throw new ArgumentException("At least one model property must be specified.", nameof(modelProps));
        }

        var valueGetter = filterProp.Compile();
        HandleField(filterProp, (prop, filter) =>
        {
            var value = valueGetter(filter);
            QueryContainer? acc = null;
            foreach (var modelProp in modelProps)
            {
                var qc = CreateResultFromFilterProperty(prop, modelProp, value);
                if (qc == null)
                {
                    continue;
                }

                acc = acc == null ? qc : acc || qc;
            }

            return acc;
        });
    }

    /// <summary>
    /// Register a filter property whose value must match all of the supplied model properties
    /// (logical AND).
    /// </summary>
    protected void AndGroup<TFilterProp>(
        Expression<Func<TFilter, TFilterProp>> filterProp,
        params Expression<Func<TModel, TFilterProp>>[] modelProps)
    {
        ArgumentNullException.ThrowIfNull(filterProp);
        ArgumentNullException.ThrowIfNull(modelProps);
        if (modelProps.Length == 0)
        {
            throw new ArgumentException("At least one model property must be specified.", nameof(modelProps));
        }

        var valueGetter = filterProp.Compile();
        HandleField(filterProp, (prop, filter) =>
        {
            var value = valueGetter(filter);
            QueryContainer? acc = null;
            foreach (var modelProp in modelProps)
            {
                var qc = CreateResultFromFilterProperty(prop, modelProp, value);
                if (qc == null)
                {
                    continue;
                }

                acc = acc == null ? qc : acc && qc;
            }

            return acc;
        });
    }

    /// <inheritdoc />
    QueryContainer ISpecification.CreateFilterQuery(object filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return CreateFilterQuery((TFilter)filter);
    }
}
