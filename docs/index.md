# XSpecification

XSpecification is a small, opinionated implementation of the Specification pattern for .NET 8 / .NET 10.

It lets you describe a query as a strongly-typed *filter* class and reuse the same specification for two
backends:

- **LINQ** — produces `Expression<Func<TModel, bool>>` for `IQueryable<T>` (EF Core, in-memory, ...).
- **Elasticsearch** — produces a `QueryContainer` (NEST 7.x) or `Query` (Elastic.Clients.Elasticsearch 8.x).

```csharp
public sealed class OrderFilter
{
    public StringFilter Buyer { get; set; } = new();
    public RangeFilter<DateTime> CreatedAt { get; set; } = new();
    public ListFilter<int> StatusIn { get; set; } = new();
}

public sealed class OrderSpec : SpecificationBase<Order, OrderFilter>
{
    public OrderSpec(
        ILogger<OrderSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<Order> pipeline)
        : base(logger, configuration, pipeline)
    {
        HandleField(f => f.Buyer, m => m.BuyerName);
    }
}
```

## Why

- Single canonical place to describe filtering rules — instead of a soup of `if (filter.X != null)` checks.
- Identical filter contract across LINQ and Elasticsearch backends.
- Strong typing: filter properties are bound to model properties at compile time.
- Performance: pipelines and reflection accessors are cached; the source generator can eliminate
  reflection altogether.

Continue with [Getting started](getting-started.md) or jump to the
[Migration guide](migration.md) if you are upgrading from 1.x.
