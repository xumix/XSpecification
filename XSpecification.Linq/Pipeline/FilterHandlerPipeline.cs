using XSpecification.Core.Pipeline;
using XSpecification.Linq.Handlers;

namespace XSpecification.Linq.Pipeline;

internal class FilterHandlerPipeline<TModel>
    : FilterHandlerPipelineBase<LinqFilterContext<TModel>>,
        IFilterHandlerPipeline<TModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterHandlerPipeline{TModel}"/> class.
    /// </summary>
    public FilterHandlerPipeline(IServiceProvider serviceProvider, LinqFilterHandlerCollection handlers)
        : base(serviceProvider, handlers)
    {
    }

    protected override bool CanHandle(object handler, LinqFilterContext<TModel> context)
    {
        return ((IFilterHandler)handler).CanHandle(context);
    }

    protected override void Handle(
        object handler,
        LinqFilterContext<TModel> context,
        Action<LinqFilterContext<TModel>> next)
    {
        ((IFilterHandler)handler).Handle(context, next);
    }
}
