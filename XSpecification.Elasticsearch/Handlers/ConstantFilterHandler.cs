using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using Nest;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace XSpecification.Elasticsearch.Handlers;

public partial class ConstantFilterHandler : IFilterHandler
{
    private readonly ILogger<ConstantFilterHandler> _logger;

    public ConstantFilterHandler(ILogger<ConstantFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        var ret = GetQuery(context);
        context.QueryContainer = context.QueryContainer && ret;

        LogCreatedConstantExpressionQuery(ret.ToPrettyString());

        next(context);
    }

    public virtual bool CanHandle(QueryContext context)
    {
        return context.FilterPropertyValue is not IFilter;
    }

    protected internal static QueryContainer GetQuery(QueryContext context)
    {
        QueryBase query = new TermQuery { Field = context.IndexFieldName, Value = context.FilterPropertyValue };
        if (context.DisableScoring)
        {
            query = +query;
        }

        return query;
    }

    [LoggerMessage(LogLevel.Debug, "Created Constant expression: {Query}")]
    partial void LogCreatedConstantExpressionQuery(string query);
}
