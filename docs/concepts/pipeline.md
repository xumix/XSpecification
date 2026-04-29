# Pipeline

When `SpecificationBase` encounters a filter property it builds a *context* and pushes it through
a chain-of-responsibility pipeline. Each link is an `IFilterHandler` registered for one of the
filter primitives:

```
ConstantFilterHandler       → exact equality, string equality, etc.
EnumerableFilterHandler     → IEnumerable<T> source values
NullableFilterHandler       → INullableFilter primitives (Range, List, ...)
ListFilterHandler           → ListFilter<T>
StringFilterHandler         → StringFilter
RangeFilterHandler          → RangeFilter<T>
```

The order is significant — earlier handlers can short-circuit subsequent ones.

## Customising the pipeline

```csharp
services.AddLinqSpecification(cfg =>
{
    cfg.AddSpecification<CustomerSpec>();

    // Inject your own handler before the built-in StringFilterHandler.
    cfg.FilterHandlers.AddBefore<StringFilterHandler>(typeof(MyCustomStringHandler));
});
```

Handlers are registered as singletons. `FilterHandlerPipelineBase` resolves them once per first
use and caches the resulting delegate behind a `Lazy<Action<TContext>>` with
`LazyThreadSafetyMode.ExecutionAndPublication`, so the pipeline is safe for concurrent use from
many threads after construction.

## Implementing a handler

```csharp
public sealed class MyHandler : IFilterHandler
{
    public void Handle<TModel>(LinqFilterContext<TModel> ctx, Action<LinqFilterContext<TModel>> next)
    {
        if (ctx.FilterPropertyValue is MyFilter mf && mf.HasValue())
        {
            ctx.Expression = BuildExpression<TModel>(ctx, mf);
            return; // short-circuit
        }

        next(ctx);
    }
}
```

Always either set the result on the context (and return) or call `next(ctx)`.
