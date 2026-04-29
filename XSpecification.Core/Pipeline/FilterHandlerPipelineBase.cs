using Microsoft.Extensions.DependencyInjection;

namespace XSpecification.Core.Pipeline;

public abstract class FilterHandlerPipelineBase<TContext>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFilterHandlerCollection _handlers;
    private Action<TContext>? _entryPoint;

    private static readonly Action<TContext> TerminateAction = _ => { };

    protected FilterHandlerPipelineBase(
        IServiceProvider serviceProvider,
        IFilterHandlerCollection handlers)
    {
        _serviceProvider = serviceProvider;
        _handlers = handlers;
    }

    public void Execute(TContext context)
    {
        _entryPoint ??= BuildPipeline();
        _entryPoint(context);
    }

    protected abstract bool CanHandle(object handler, TContext context);

    protected abstract void Handle(object handler, TContext context, Action<TContext> next);

    private Action<TContext> BuildPipeline()
    {
        var current = _handlers.Last;
        var currentInvoke = TerminateAction;

        Action<TContext> Chain(Action<TContext> next, Type handlerType) =>
            ctxt =>
            {
                var handler = _serviceProvider.GetRequiredService(handlerType);

                if (CanHandle(handler, ctxt))
                {
                    Handle(handler, ctxt, next);
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
