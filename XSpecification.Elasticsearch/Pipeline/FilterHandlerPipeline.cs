using XSpecification.Core.Pipeline;
using XSpecification.Elasticsearch.Handlers;

namespace XSpecification.Elasticsearch.Pipeline;

#pragma warning disable CA1812

internal sealed class FilterHandlerPipeline
    : FilterHandlerPipelineBase<QueryContext>,
        IFilterHandlerPipeline
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterHandlerPipeline{TModel}"/> class.
    /// </summary>
    public FilterHandlerPipeline(IServiceProvider serviceProvider, ElasticFilterHandlerCollection handlers)
        : base(serviceProvider, handlers)
    {
    }

    protected override bool CanHandle(object handler, QueryContext context)
    {
        return ((IFilterHandler)handler).CanHandle(context);
    }

    protected override void Handle(
        object handler,
        QueryContext context,
        Action<QueryContext> next)
    {
        ((IFilterHandler)handler).Handle(context, next);
    }
}
