using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Nest;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

using Context = XSpecification.Core.Pipeline.Context;

namespace XSpecification.Elasticsearch.Handlers;

public class EnumerableFilterHandler : IFilterHandler
{
    private readonly ILogger<EnumerableFilterHandler> _logger;

    public EnumerableFilterHandler(ILogger<EnumerableFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        var ret = GetQuery(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Enumerable expression: {Query}", ret.ToPrettyString());

            context.QueryContainer = context.QueryContainer && ret;
        }

        next(context);
    }

    public virtual bool CanHandle(QueryContext context)
    {
        var value = context.FilterPropertyValue!;
        return value is IEnumerable && value is not string && value is not IListFilter;
    }

    protected internal static QueryContainer GetQuery(QueryContext context)
    {
        var propertyType = context.ModelProperty!.PropertyType;
        var enumerable = (IEnumerable)context.FilterPropertyValue!;
        var fieldName = context.IndexFieldName;

        // Check if the property type is the same as the filter type
        var terms = enumerable.ToArray(propertyType)!.Cast<object>().ToArray();

        //Elastic may lag in the query of where in() with a single element
        QueryBase query = terms.Length == 1
            ? new TermQuery { Field = fieldName, Value = terms[0] }
            : new TermsQuery { Field = fieldName, Terms = terms };

        if (!terms.Any())
        {
            query.IsVerbatim = true;
        }

        //query.Boost = boost;

        if (context.DisableScoring)
        {
            query = +query;
        }

        return query;
    }
}
