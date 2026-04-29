using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Linq.Pipeline;

using Options = XSpecification.Core.Options;

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
        IOptions<Options> options,
        IFilterHandlerPipeline<TModel> handlerPipeline)
        : base(options.Value.DisablePropertyAutoHandling)
    {
        _logger = logger;
        _handlerPipeline = handlerPipeline;
    }

    public virtual Expression<Func<TModel, bool>> CreateFilterExpression(TFilter filter)
    {
        return CreateFilterResult(filter);
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

    /// <inheritdoc />
    LambdaExpression ISpecification.CreateFilterExpression(object filter)
    {
        return CreateFilterExpression((TFilter)filter);
    }
}
