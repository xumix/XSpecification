using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Core.Pipeline;

public abstract class FilterHandlerPipelineBase<TContext>
{
    private static readonly Action<TContext> TerminateAction = static _ => { };

    private readonly IServiceProvider _serviceProvider;
    private readonly IFilterHandlerCollection _handlers;
    private readonly Lazy<Action<TContext>> _entryPoint;

    protected FilterHandlerPipelineBase(
        IServiceProvider serviceProvider,
        IFilterHandlerCollection handlers)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(handlers);

        _serviceProvider = serviceProvider;
        _handlers = handlers;

        // ExecutionAndPublication ensures the pipeline is built exactly once
        // even under concurrent first-use.
        _entryPoint = new Lazy<Action<TContext>>(BuildPipeline, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void Execute(TContext context)
    {
        _entryPoint.Value(context);
    }

    protected abstract bool CanHandle(object handler, TContext context);

    protected abstract void Handle(object handler, TContext context, Action<TContext> next);

    private Action<TContext> BuildPipeline()
    {
        var currentInvoke = TerminateAction;

        // Resolve singleton handler instances exactly once per pipeline build,
        // so we don't pay GetRequiredService() on every Execute() call.
        foreach (var handlerType in _handlers.EnumerateReversed())
        {
            var handler = _serviceProvider.GetRequiredService(handlerType);
            currentInvoke = Chain(currentInvoke, handler);
        }

        return currentInvoke;
    }

    private Action<TContext> Chain(Action<TContext> next, object handler) =>
        ctxt =>
        {
            if (CanHandle(handler, ctxt))
            {
                Handle(handler, ctxt, next);
            }
            else
            {
                next(ctxt);
            }
        };
}
