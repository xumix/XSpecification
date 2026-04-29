using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Nest;

using XSpecification.Core;
using XSpecification.Elasticsearch.Pipeline;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace XSpecification.Elasticsearch.Handlers;

public partial class StringFilterHandler : IFilterHandler
{
    private readonly ILogger<StringFilterHandler> _logger;

    public StringFilterHandler(ILogger<StringFilterHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        var ret = GetQuery(context);
        if (ret != default)
        {
            LogCreatedStringExpressionQuery(ret.ToPrettyString());

            context.QueryContainer = context.QueryContainer && ret;
        }

        next(context);
    }

    public virtual bool CanHandle(QueryContext context)
    {
        if (!typeof(StringFilter).IsAssignableFrom(context.FilterProperty!.PropertyType))
        {
            return false;
        }

        return true;
    }

    protected static QueryContainer? GetQuery(QueryContext context)
    {
        var stringFilter = (StringFilter)context.FilterPropertyValue!;
        var isFullText = context.ModelProperty!.GetCustomAttribute<TextAttribute>() != null;
        var fieldName = context.IndexFieldName;

        if (!stringFilter.HasValue())
        {
            return null;
        }

        QueryBase? query =  stringFilter switch
        {
            { Contains: true } => isFullText
                ? new MatchQuery { Field = fieldName, Query = stringFilter.Value }
                : new WildcardQuery { Field = fieldName, Value = $"*{stringFilter.Value}*" },
            { StartsWith: true } => new WildcardQuery { Field = fieldName, Value = $"{stringFilter.Value}*" },
            { EndsWith: true } => new WildcardQuery { Field = fieldName, Value = $"*{stringFilter.Value}" },
            _ => null
        };

        if (query == null)
        {
            query = isFullText
                ? new MatchPhraseQuery { Field = fieldName, Query = stringFilter.Value }
                : new TermQuery { Field = fieldName, Value = stringFilter.Value };
        }

        if (stringFilter.IsInverted)
        {
            query = !query;
        }

        if (!isFullText && context.DisableScoring)
        {
            query = +query;
        }

        return query;
    }

    [LoggerMessage(LogLevel.Debug, "Created String expression: {Query}")]
    partial void LogCreatedStringExpressionQuery(string query);
}
