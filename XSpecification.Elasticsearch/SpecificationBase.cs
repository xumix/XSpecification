using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nest;

using Newtonsoft.Json.Serialization;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

using Options = XSpecification.Core.Options;

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
        IOptions<Options> options,
        IFilterHandlerPipeline handlerPipeline)
        : base(options.Value.DisablePropertyAutoHandling)
    {
        _logger = logger;
        _handlerPipeline = handlerPipeline;
    }

    public virtual QueryContainer CreateFilterQuery(TFilter filter)
    {
        return CreateFilterResult(filter);
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


    /// <inheritdoc />
    QueryContainer ISpecification.CreateFilterQuery(object filter)
    {
        return CreateFilterQuery((TFilter)filter);
    }
}
