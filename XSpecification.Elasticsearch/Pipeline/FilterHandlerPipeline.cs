using Microsoft.Extensions.DependencyInjection;

using XSpecification.Core.Pipeline;
using XSpecification.Elasticsearch.Handlers;

namespace XSpecification.Elasticsearch.Pipeline;

internal class FilterHandlerPipeline : IFilterHandlerPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFilterHandlerCollection _handlers;
    private Action<QueryContext>? _entryPoint;

    /// <summary>
    /// Termination action for the end of pipelines.
    /// </summary>
    private static readonly Action<QueryContext> TerminateAction = ctxt => { };

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterHandlerPipeline{TModel}"/> class.
    /// </summary>
    public FilterHandlerPipeline(IServiceProvider serviceProvider, ElasticFilterHandlerCollection handlers)
    {
        _serviceProvider = serviceProvider;
        _handlers = handlers;
    }

    /// <inheritdoc />
    public void Execute(QueryContext ctxt)
    {
        _entryPoint ??= BuildPipeline();
        _entryPoint?.Invoke(ctxt);
    }

    /// <summary>
    /// Builds the pipeline as a chain of actions.
    /// </summary>
    /// <returns></returns>
    public Action<QueryContext> BuildPipeline()
    {
        // When we build, we go through the set and construct a single call stack, from the end.
        var current = _handlers.Last;
        var currentInvoke = TerminateAction;

        Action<QueryContext> Chain(
            Action<QueryContext> next,
            Type handlerType) =>
            ctxt =>
            {
                var converter = (IFilterHandler)_serviceProvider.GetRequiredService(handlerType);

                if (converter.CanHandle(ctxt))
                {
                    converter.Handle(ctxt, next);
                }
                else
                {
                    next(ctxt);
                }
            };

        while (current != null)
        {
            var handlerType = current.Value;
            currentInvoke = Chain(currentInvoke, handlerType);
            current = current.Previous;
        }

        return currentInvoke;
    }
}
