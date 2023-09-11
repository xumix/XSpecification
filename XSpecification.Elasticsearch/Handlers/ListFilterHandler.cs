using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using Nest;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elasticsearch.Handlers;

public class ListFilterHandler : IFilterHandler
{
    private readonly ILogger<ListFilterHandler> _logger;

    public ListFilterHandler(ILogger<ListFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        var ret = GetQuery(context);
        if (ret != default)
        {
            _logger.LogDebug("Created List expression: {Query}", ret.ToPrettyString());
            context.QueryContainer = context.QueryContainer && ret;
        }

        next(context);
    }

    public virtual bool CanHandle(QueryContext context)
    {
        if (!typeof(IListFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static QueryContainer? GetQuery(QueryContext context)
    {
        var value = (IListFilter)context.FilterPropertyValue!;

        if (!value.HasValue())
        {
            return null;
        }

        var query = EnumerableFilterHandler.GetQuery(context);

        if (value.IsInverted)
        {
            query = !query;
        }

        return query;
    }
}
