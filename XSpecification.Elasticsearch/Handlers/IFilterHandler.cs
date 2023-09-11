using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elasticsearch.Handlers;

public interface IFilterHandler
{
    void Handle(QueryContext context, Action<QueryContext> next);

    bool CanHandle(QueryContext context);
}
