# XSpecification

[![CI](https://github.com/xumix/XSpecification/actions/workflows/dotnet.yml/badge.svg)](https://github.com/xumix/XSpecification/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/xumix/XSpecification/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/xumix/XSpecification/actions/workflows/codeql-analysis.yml)
[![NuGet (Linq)](https://img.shields.io/nuget/v/XSpecification.Linq.svg?label=XSpecification.Linq)](https://www.nuget.org/packages/XSpecification.Linq)
[![NuGet (Elasticsearch)](https://img.shields.io/nuget/v/XSpecification.Elasticsearch.svg?label=XSpecification.Elasticsearch)](https://www.nuget.org/packages/XSpecification.Elasticsearch)
[![codecov](https://codecov.io/gh/xumix/XSpecification/branch/master/graph/badge.svg)](https://codecov.io/gh/xumix/XSpecification)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

XSpecification is an implementation of the Specification pattern for both LINQ (EF Core / `IQueryable<T>`) and Elasticsearch (NEST 7.x and Elastic.Clients.Elasticsearch 8.x).

> Full reference, concepts, and API guides are available in the [docs](docs/index.md). New to 2.0? Read the [migration guide](MIGRATION.md).

It removes the boilerplate of "if has value then where ..." chains and standardises the most common filter shapes:

- **`RangeFilter<T>`** — `BETWEEN x AND y` (inclusive / exclusive, with start-as-equals shortcut).
- **`ListFilter<T>`** — `IN (x, y, z)` (with `IsInverted` for `NOT IN`).
- **`StringFilter`** — `=`, `LIKE '%x%'`, `LIKE 'x%'`, `LIKE '%x'`, `IS NULL`, `IS NOT NULL`.
- Direct primitive comparison and nullable-aware `IS NULL` / `IS NOT NULL` for any property.

Useful when you have many list / report screens with similar filtering, or in BL-heavy apps where you find yourself writing this kind of code:

```csharp
public class SomeApiFilter
{
    public DateTime? Date { get; set; }
    public string Name { get; set; }
    public string NameContains { get; set; }
    public int? IdFrom { get; set; }
    public int? IdTo { get; set; }
}

var where = PredicateBuilder.New<DbModel>();
if (filter.Date.HasValue)
    where.And(f => f.Date == filter.Date.Value);
if (!string.IsNullOrEmpty(filter.Name))
    where.And(f => f.Name == filter.Name);
if (!string.IsNullOrEmpty(filter.NameContains))
    where.And(f => f.Name.Contains(filter.NameContains));
if (filter.IdFrom.HasValue)
    where.And(f => f.Id >= filter.IdFrom);
if (filter.IdTo.HasValue)
    where.And(f => f.Id <= filter.IdTo);

var data = dbcontext.Set<DbModel>().Where(where);
```

With XSpecification it becomes:

```csharp
var expression = spec.CreateFilterExpression(filter);
var data = dbcontext.Set<DbModel>().Where(expression);
```

---

## Setup — LINQ backend

Define a filter (use the canonical filter primitives where you need shape-aware behaviour):

```csharp
public class LinqTestFilter
{
    public int? Id { get; set; }
    public StringFilter Name { get; set; }
    public RangeFilter<int> RangeId { get; set; }
    public ListFilter<int> ListId { get; set; }
    public string Explicit { get; set; }
    public bool Conditional { get; set; }
    public string Ignored { get; set; }
}
```

Define your specification. **If a filter property has the same name as a model property, it is mapped automatically.** Use `HandleField` for explicit mappings or conditional logic, `IgnoreField` to skip filter properties.

```csharp
public class LinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
{
    public LinqTestSpec(
        ILogger<LinqTestSpec> logger,
        IOptions<Options> options,
        IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
        IgnoreField(f => f.Ignored);

        // Map a filter property to a differently-named model property
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);

        // Conditional / fluent expression building
        HandleField(f => f.Conditional, (prop, filter) =>
        {
            if (filter.Conditional)
                return CreateExpressionFromFilterProperty(prop, m => m.Name, filter.Conditional.ToString());

            if (!filter.Conditional && filter.Id == 312)
                return PredicateBuilder.New<LinqTestModel>()
                                       .And(m => m.Date.Hour == 1)
                                       .And(m => m.UnmatchingProperty == 123);

            return null;
        });
    }
}
```

Wire it up in DI:

```csharp
services.AddLinqSpecification(cfg =>
{
    cfg.AddSpecification<LinqTestSpec>();
    // optionally tweak the pipeline:
    // cfg.FilterHandlers.AddBefore<ConstantFilterHandler>(typeof(MyCustomHandler));
});

// Optionally validate every registered specification at app start (catches misconfigurations early):
provider.ValidateSpecifications();
```

If you prefer to register handlers explicitly per filter type and skip convention-based property mapping:

```csharp
services.AddLinqSpecification(cfg =>
{
    cfg.AddSpecification<LinqTestSpec>();
}).Configure(o => o.DisablePropertyAutoHandling = true);
```

Use the spec:

```csharp
var spec = serviceProvider.GetRequiredService<LinqTestSpec>();

var filter = new LinqTestFilter
{
    Date         = DateTime.Today,
    Id           = 123,
    Name         = "qwe",
    ComplexName  = new StringFilter("complex") { Contains = true },
    ListDate     = new[] { DateTime.Today, DateTime.Today.AddDays(1) },
    ListId       = new[] { 1, 2, 3 },
    ListName     = new ListFilter<string>("a", "b", "z") { IsInverted = true },
    NullableDate = DateTime.Today.AddDays(-1),
    RangeDate    = new RangeFilter<DateTime> { Start = DateTime.Today, End = DateTime.Today.AddDays(1) },
    RangeId      = new RangeFilter<int> { Start = 0, End = 5 },
};

var expression = spec.CreateFilterExpression(filter);
var data = dbContext.Set<LinqTestModel>().Where(expression);

/* roughly translates to:
SELECT ... FROM LinqTestModel
WHERE Date = @date AND Id = 123 AND ComplexName LIKE '%complex%'
  AND ListDate IN (@d1, @d2) AND ListId IN (1, 2, 3)
  AND ListName NOT IN ('a', 'b', 'z') AND NullableDate = @nd
  AND RangeDate >= @rds AND RangeDate <= @rde AND RangeId >= 0 AND RangeId <= 5
*/
```

---

## Setup — Elasticsearch (NEST 7.x) backend

```csharp
public class ProductSpec : SpecificationBase<ProductDoc, ProductFilter>
{
    public ProductSpec(
        ILogger<ProductSpec> logger,
        IOptions<Options> options,
        IFilterHandlerPipeline handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
        IgnoreField(f => f.Ignored);
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
    }
}

services.AddElasticSpecification(cfg =>
{
    cfg.AddSpecification<ProductSpec>();
});

// Build a query
var spec = sp.GetRequiredService<ProductSpec>();
QueryContainer query = spec.CreateFilterQuery(filter);

// Use it with the NEST client
var response = await elasticClient.SearchAsync<ProductDoc>(s => s.Query(_ => query));
```

The Elasticsearch backend understands `[Text]`, `[Number]`, `[Date]` NEST attributes on the model and switches between `MatchQuery`, `WildcardQuery`, `TermQuery`, `DateRangeQuery` and `NumericRangeQuery` accordingly.

> An Elasticsearch 8.x backend (built on `Elastic.Clients.Elasticsearch`) is planned for the upcoming 2.0 release; it will live in a separate `XSpecification.Elasticsearch.V8` package.

---

## Filter primitives reference

| Type | Behaviour |
| --- | --- |
| `StringFilter` | Equality / `LIKE` / null check / inversion. |
| `RangeFilter<T>` | `[start, end]` with optional `IsExclusive`, `UseStartAsEquals` shortcut, null check. |
| `ListFilter<T>` | `IN (...)` with optional `IsInverted` and null check. |
| `INullableFilter` | Marker for any filter that can express `IS NULL` / `IS NOT NULL`. |
| Plain primitives | Treated as direct equality if a value is present (`null` / empty string == "no filter"). |

You can add custom filter handlers by implementing `IFilterHandler` and registering them via `cfg.FilterHandlers.AddBefore<...>` / `AddAfter<...>` / `AddFirst` / `AddLast`.

---

## Roadmap

- [x] Add extension points for new filter types.
- [x] Refactor expression-creation logic to a Chain of Responsibility pipeline.
- [x] Add Elasticsearch (NEST 7.x) implementation.
- [ ] **Phase 2** — performance and bug-fix release: `FilterHandlerPipeline` thread-safety, cached compiled delegates instead of reflection on the hot path, baseline benchmarks, English-only docs and identifier cleanup. (Tracked in [`CHANGELOG.md`](CHANGELOG.md).)
- [ ] **Phase 3 / 2.0** — multi-targeting `net8.0;net10.0`, source-generator-based AOT-friendly path, parallel support for `Elastic.Clients.Elasticsearch` 8.x next to NEST 7.x, public API cleanup, `BoolFilter`, fluent `OrGroup`/`AndGroup` and spec composition.

---

## Building and contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). The TL;DR:

```powershell
dotnet build XSpecification.slnx -c Release
dotnet test  XSpecification.slnx -c Release
```

The build uses Central Package Management ([`Directory.Packages.props`](Directory.Packages.props)) and runs SourceLink, StyleCop and Roslynator analyzers with `TreatWarningsAsErrors`.
