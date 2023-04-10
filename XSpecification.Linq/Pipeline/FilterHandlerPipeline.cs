using Microsoft.Extensions.DependencyInjection;

using XSpecification.Linq.Handlers;

namespace XSpecification.Linq.Pipeline;

internal class FilterHandlerPipeline<TModel> : IFilterHandlerPipeline<TModel>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFilterHandlerCollection _handlers;
    private Action<Context<TModel>>? _entryPoint;

    /// <summary>
    /// Termination action for the end of pipelines.
    /// </summary>
    private static readonly Action<Context<TModel>> TerminateAction = ctxt => { };

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterHandlerPipeline{TModel}"/> class.
    /// </summary>
    public FilterHandlerPipeline(IServiceProvider serviceProvider, IFilterHandlerCollection handlers)
    {
        _serviceProvider = serviceProvider;
        _handlers = handlers;
    }

    /// <inheritdoc />
    public void Execute(Context<TModel> ctxt)
    {
        _entryPoint ??= BuildPipeline();
        _entryPoint?.Invoke(ctxt);
    }

    /// <summary>
    /// Builds the pipeline as a chain of actions.
    /// </summary>
    /// <returns></returns>
    public Action<Context<TModel>> BuildPipeline()
    {
        // When we build, we go through the set and construct a single call stack, from the end.
        var current = _handlers.Last;
        var currentInvoke = TerminateAction;

        Action<Context<TModel>> Chain(
            Action<Context<TModel>> next,
            Type handlerType) =>
            ctxt =>
            {
                var converter = (IFilterHandler)_serviceProvider.GetRequiredService(handlerType);

                if (converter.CanHandle(ctxt))
                {
                    converter.CreateExpression(ctxt, next);
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
