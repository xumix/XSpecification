# Migration guide — XSpecification 1.x → 2.0

XSpecification 2.0 is a major release with **breaking changes**. This guide walks you through the
upgrade with diff-style examples.

> **TL;DR** — replace `IOptions<Options>` with `SpecificationConfiguration` in your specification
> constructors, use the new `BoolFilter` / `OrGroup` helpers if applicable, switch to the
> `XSpecification.Elasticsearch.Nest` package id if you depend on the NEST 7.x backend.

## 1. Target framework

XSpecification 2.0 multi-targets **`net8.0`** and **`net10.0`**. If you still build for `net6.0`,
stay on the 1.x line.

## 2. `IOptions<Options>` → `SpecificationConfiguration`

The 1.x base class accepted `IOptions<Options>` from `Microsoft.Extensions.Options`. 2.0
introduces a dedicated `SpecificationConfiguration` record without a dependency on
`Microsoft.Extensions.Options.ConfigurationExtensions`.

```diff
 public sealed class CustomerSpec : SpecificationBase<Customer, CustomerFilter>
 {
     public CustomerSpec(
         ILogger<CustomerSpec> logger,
-        IOptions<Options> options,
+        SpecificationConfiguration configuration,
         IFilterHandlerPipeline<Customer> pipeline)
-        : base(logger, options, pipeline)
+        : base(logger, configuration, pipeline)
     {
     }
 }
```

The legacy `IOptions<Options>` constructor is still available behind `[Obsolete]` and will be
removed in 3.0.

The DI extension signature changed too:

```diff
 services
-    .AddLinqSpecification(cfg => cfg.AddSpecification<CustomerSpec>())
-    .Configure(o => o.DisablePropertyAutoHandling = false);
+    .AddLinqSpecification(
+        cfg => cfg.AddSpecification<CustomerSpec>(),
+        configure: c => c.DisablePropertyAutoHandling = false);
```

## 3. `FilterHandlerCollection` no longer inherits from `LinkedList<Type>`

`IFilterHandlerCollection` no longer exposes `LinkedListNode<Type>` based members. If you walked
the collection manually using `First`/`Last`/`Next`, switch to the new `EnumerateReversed()` (or
plain `IEnumerable<Type>`).

```diff
-foreach (var node = collection.First; node != null; node = node.Next)
-    Use(node.Value);
+foreach (var type in collection)
+    Use(type);
```

## 4. `UnmatchedProps` is read-only via `UnhandledFilterProperties`

`SpecificationBase.UnmatchedProps` is `[Obsolete]` and points at the same backing list. New code
should read `UnhandledFilterProperties` (an `IReadOnlyCollection<string>`) and let the runtime
manage the list through `HandleField` / `IgnoreField`.

## 5. Typo fixes

- `ExpressionExtensions.CreateClousre` → `CreateClosure` (legacy name kept as `[Obsolete]` until
  3.0).
- Test fixtures `ElsaticTestFilter` → `ElasticTestFilter` (and `IncompatibleElsaticTestFilter` →
  `IncompatibleElasticTestFilter`). Internal-only, no consumer impact unless you reference the
  test assembly directly.

## 6. New helpers

- **`BoolFilter`** — tri-state boolean filter (unset / `true` / `false`).
- **`OrGroup`** / **`AndGroup`** — fluent helpers on `SpecificationBase` that replace the manual
  pattern of calling `CreateExpressionFromFilterProperty` for each model property and combining
  the results.
- **`SpecificationCompositionExtensions.And` / `.Or`** — compose two specs sharing the same
  `TModel` / `TFilter` into a combined predicate or query.
- **Async surface** — `CreateFilterExpressionAsync(filter, ct)` and `CreateFilterQueryAsync(filter, ct)`.

```diff
 // Before
 HandleField(f => f.Search, (prop, filter) =>
 {
     var n = CreateExpressionFromFilterProperty(prop, m => m.Name, filter.Search);
     var l = CreateExpressionFromFilterProperty(prop, m => m.ListName, filter.Search);
     return n != null && l != null ? n.Or(l) : (n ?? l);
 });

 // After
 OrGroup(f => f.Search, m => m.Name, m => m.ListName);
```

## 7. Elasticsearch — package split

The 1.x package `XSpecification.Elasticsearch` is now an alias for the NEST 7.x backend. If you
explicitly want to track the NEST backend, depend on the new id:

```diff
- <PackageReference Include="XSpecification.Elasticsearch" Version="1.x" />
+ <PackageReference Include="XSpecification.Elasticsearch.Nest" Version="2.0.0" />
```

To migrate to the modern Elastic.Clients.Elasticsearch 8.x client:

```xml
<PackageReference Include="XSpecification.Elasticsearch.V8" Version="2.0.0" />
```

The shared `IQueryNode` AST in `XSpecification.Elasticsearch.Abstractions` lets you reuse
specifications across both backends.

## 8. Source generator

If you target AOT or care about reflection-free hot paths:

```bash
dotnet add package XSpecification.SourceGenerator
```

You'll get compile-time `XSPEC001` warnings for unmapped filter properties — promote those to
errors in your `.csproj` if you want hard guarantees:

```xml
<PropertyGroup>
  <WarningsAsErrors>$(WarningsAsErrors);XSPEC001</WarningsAsErrors>
</PropertyGroup>
```

## 9. Quick checklist

- [ ] Switch `IOptions<Options>` to `SpecificationConfiguration` in spec ctors.
- [ ] Rename `OptionsBuilder<Options>`-returning chains in `Add*Specification`.
- [ ] Replace `LinkedListNode<Type>` walks of `IFilterHandlerCollection`.
- [ ] Audit references to `UnmatchedProps`; switch to `UnhandledFilterProperties`.
- [ ] Run the build with `XSPEC001` warnings enabled (after adding the source generator) to find
  filter properties that silently slipped through.
- [ ] Review NEST vs ES client v8 — split your code base accordingly.
