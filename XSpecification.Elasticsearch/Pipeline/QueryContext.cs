using Nest;
using Context = XSpecification.Core.Pipeline.Context;

namespace XSpecification.Elasticsearch.Pipeline;

public class QueryContext : Context
{
    public QueryContainer QueryContainer { get; set; } = new QueryContainer();

    public string IndexFieldName { get; init; }

    public bool DisableScoring { get; init; }
}
