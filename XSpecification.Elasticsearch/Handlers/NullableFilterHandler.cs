using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using Nest;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elasticsearch.Handlers;

public class NullableFilterHandler : IFilterHandler
{
    private readonly ILogger<NullableFilterHandler> _logger;

    public NullableFilterHandler(ILogger<NullableFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        var ret = GetQuery(context);
        if (ret != default)
        {
            _logger.LogDebug("Created Nullable expression: {Query}", ret.ToPrettyString());
            context.QueryContainer = context.QueryContainer && ret;
        }
        else
        {
            next(context);
        }
    }

    public virtual bool CanHandle(QueryContext context)
    {
        if (!typeof(INullableFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static QueryContainer? GetQuery(QueryContext context)
    {
        var value = (INullableFilter)context.FilterPropertyValue!;
        var fieldName = context.IndexFieldName;

        QueryBase? query = null;

        if (value.IsNull)
        {
            query = !new ExistsQuery { Field = fieldName };
        }
        else if (value.IsNotNull)
        {
            query = new ExistsQuery { Field = fieldName };
        }

        if (query == null)
        {
            return null;
        }

        if (context.DisableScoring)
        {
            query = +query;
        }

        return query;
    }
}
