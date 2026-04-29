# Performance

## Hot-path optimisations (2.0)

| Hot path                              | Before                                                              | 2.0                                                              |
|---------------------------------------|---------------------------------------------------------------------|------------------------------------------------------------------|
| Pipeline DI resolution                | `GetRequiredService(handlerType)` per `Execute` call               | Resolved once per pipeline, cached behind `Lazy<Action<T>>`      |
| `CreateResultFromFilterProperty`      | `MakeGenericMethod` + `MethodInfo.Invoke` per call                 | Compiled `Expression.Lambda` cached in `ConcurrentDictionary`    |
| Static method-info lookups            | `IDictionary<string, MethodInfo>`                                  | `FrozenDictionary<string, MethodInfo>`                           |
| Unhandled-property check              | `Any()` per `CreateFilterResult` call                              | One-shot validation on first use                                 |

## Benchmarks

The repo includes `XSpecification.Benchmarks` (BenchmarkDotNet) with baseline scenarios:

- `LinqSmallSpec.CreateFilterExpression(filter)` — 4-property filter
- `LinqLargeSpec.CreateFilterExpression(filter)` — wider filter with explicit handlers
- `ElasticSmallSpec.CreateFilterQuery(filter)` / `ElasticLargeSpec.CreateFilterQuery(filter)`

Run them locally with:

```bash
dotnet run --project XSpecification.Benchmarks/XSpecification.Benchmarks.csproj -c Release
```

## Thread safety

`FilterHandlerPipelineBase<TContext>` builds its delegate exactly once via
`LazyThreadSafetyMode.ExecutionAndPublication`; subsequent `Execute` calls take a single field
read. The pipeline can therefore be used safely from many threads as long as the registered
handlers are themselves stateless or thread-safe, which is the case for the built-in handlers.

Specifications cache compiled dispatcher delegates in a `ConcurrentDictionary` keyed by closed
property type, so concurrent first-time use is also safe.

## AOT / trimming

The runtime path uses reflection (`MakeGenericMethod`, `Expression.Compile`) so it is not
trimming/AOT compatible by default. The base class is annotated with `[RequiresUnreferencedCode]`
and `[RequiresDynamicCode]` so the compiler will warn AOT consumers.

For AOT-friendly setups, plug in `XSpecification.SourceGenerator`: it emits compile-time accessor
delegates and warns about unmapped filter properties, eliminating reflection.
