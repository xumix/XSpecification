using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq;

public abstract class SpecificationBase<TModel, TFilter>
    : XSpecification.Core.SpecificationBase<TModel, TFilter, Expression<Func<TModel, bool>>>,
        ISpecification
    where TFilter : class, new()
    where TModel : class, new()
{
    private readonly ILogger<SpecificationBase<TModel, TFilter>> _logger;
    private readonly IFilterHandlerPipeline<TModel> _handlerPipeline;

    protected SpecificationBase(
        ILogger<SpecificationBase<TModel, TFilter>> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<TModel> handlerPipeline)
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
        IFilterHandlerPipeline<TModel> handlerPipeline)
        : this(
            logger,
            (options ?? throw new ArgumentNullException(nameof(options))).Value.ToConfiguration(),
            handlerPipeline)
    {
    }

    public virtual Expression<Func<TModel, bool>> CreateFilterExpression(TFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return CreateFilterResult(filter);
    }

    /// <summary>
    /// Asynchronous variant of <see cref="CreateFilterExpression"/>. The current LINQ backend builds
    /// expressions synchronously; the async signature is provided as a forward-compatible contract
    /// (e.g. for future I/O-bound visitors) and respects <paramref name="cancellationToken"/>.
    /// </summary>
    public virtual ValueTask<Expression<Func<TModel, bool>>> CreateFilterExpressionAsync(
        TFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(CreateFilterExpression(filter));
    }

    protected virtual Expression<Func<TModel, bool>>? CreateExpressionFromFilterProperty<TProperty>(
        PropertyInfo filterProp,
        Expression<Func<TModel, TProperty>>? modelProp,
        object? sourceValue)
    {
        return CreateResultFromFilterProperty(filterProp, modelProp, sourceValue);
    }

    protected override Expression<Func<TModel, bool>> CombineResults(
        IReadOnlyCollection<Expression<Func<TModel, bool>>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var result = PredicateBuilder.New<TModel>(true);
        foreach (var expression in results)
        {
            result.And(expression);
        }

        var optimized = ExpressionOptimizer.tryVisit(result);
        return (Expression<Func<TModel, bool>>)optimized;
    }

    protected override Expression<Func<TModel, bool>>? CreateResultFromFilterProperty<TProperty>(
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

            var context = new LinqFilterContext<TModel>
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

    /// <summary>
    /// Register a filter property whose value is matched against any of the supplied model
    /// properties (logical OR). Replaces the manual pattern of calling
    /// <c>CreateExpressionFromFilterProperty</c> for each model property and OR-ing the results.
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
            Expression<Func<TModel, bool>>? acc = null;
            foreach (var modelProp in modelProps)
            {
                var expr = CreateResultFromFilterProperty(prop, modelProp, value);
                if (expr == null)
                {
                    continue;
                }

                acc = acc == null ? expr : acc.Or(expr);
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
            Expression<Func<TModel, bool>>? acc = null;
            foreach (var modelProp in modelProps)
            {
                var expr = CreateResultFromFilterProperty(prop, modelProp, value);
                if (expr == null)
                {
                    continue;
                }

                acc = acc == null ? expr : acc.And(expr);
            }

            return acc;
        });
    }

    /// <inheritdoc />
    LambdaExpression ISpecification.CreateFilterExpression(object filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return CreateFilterExpression((TFilter)filter);
    }
}
