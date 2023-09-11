using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Nest;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elasticsearch.Handlers;

public class RangeFilterHandler : IFilterHandler
{
    private readonly ILogger<RangeFilterHandler> _logger;

    public RangeFilterHandler(ILogger<RangeFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        var ret = GetQuery(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Range query: {Query}", ret.ToPrettyString());
            context.QueryContainer = context.QueryContainer && ret;
        }

        next(context);
    }

    public virtual bool CanHandle(QueryContext context)
    {
        if (!typeof(IRangeFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected internal QueryContainer? GetQuery(QueryContext context)
    {
        var rangeFilter = (IRangeFilter)context.FilterPropertyValue!;
        var fieldName = context.IndexFieldName;
        var boost = context.ModelProperty!.GetCustomAttribute<NumberAttribute>()?.Boost
            ?? context.ModelProperty!.GetCustomAttribute<DateAttribute>()?.Boost;

        if (!rangeFilter.HasValue())
        {
            return null;
        }

        QueryBase query;

        if (rangeFilter.ElementType == typeof(DateTime))
        {
            if (rangeFilter.UseStartAsEquals && rangeFilter.Start != null)
            {
                query = new TermQuery { Field = context.IndexFieldName, Value = rangeFilter.Start };
            }
            else
            {
                var dateQuery = new DateRangeQuery { Field = context.IndexFieldName };

                if (rangeFilter.Start != null)
                {
                    dateQuery.GreaterThanOrEqualTo = (DateTime)rangeFilter.Start;
                }

                if (rangeFilter.End != null)
                {
                    dateQuery.LessThanOrEqualTo = (DateTime)rangeFilter.End;
                }

                query = dateQuery;
            }
        }
        else
        {
            if (rangeFilter.UseStartAsEquals && rangeFilter.Start != null)
            {
                query = new TermQuery { Field = fieldName, Value = Convert.ToDouble(rangeFilter.Start) };
            }
            else
            {
                var numQuery = new NumericRangeQuery { Field = fieldName };

                if (rangeFilter.Start != null)
                {
                    numQuery.GreaterThanOrEqualTo = Convert.ToDouble(rangeFilter.Start);
                }

                if (rangeFilter.End != null)
                {
                    numQuery.LessThanOrEqualTo = Convert.ToDouble(rangeFilter.End);
                }

                query = numQuery;
            }
        }

        if (context.DisableScoring)
        {
            query = +query;
        }

        query.Boost = boost;

        return query;
    }
}
