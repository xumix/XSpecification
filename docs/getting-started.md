# Getting started

## Install

=== "LINQ backend"

    ```bash
    dotnet add package XSpecification.Linq
    ```

=== "Elasticsearch (NEST 7.x)"

    ```bash
    dotnet add package XSpecification.Elasticsearch.Nest
    ```

=== "Elasticsearch (Elastic.Clients.Elasticsearch 8.x)"

    ```bash
    dotnet add package XSpecification.Elasticsearch.V8
    ```

## Define a filter

```csharp
using XSpecification.Core;

public sealed class CustomerFilter
{
    public StringFilter Name { get; set; } = new();
    public RangeFilter<DateTime> RegisteredAt { get; set; } = new();
    public ListFilter<int> StatusIn { get; set; } = new();
    public BoolFilter IsVip { get; set; } = new();
}
```

The primitives in `XSpecification.Core` cover the most common filter shapes:

| Primitive          | Use case                                                |
|--------------------|---------------------------------------------------------|
| `StringFilter`     | starts-with / ends-with / contains / equals on strings  |
| `RangeFilter<T>`   | inclusive/exclusive numeric or date ranges, null checks |
| `ListFilter<T>`    | "value is in this set" (terms query / `Contains`)       |
| `BoolFilter`       | tri-state boolean: unset, `true`, `false`               |
| any `INullable`    | explicit null/not-null checks                            |

## Define a specification

```csharp
using Microsoft.Extensions.Logging;
using XSpecification.Core;
using XSpecification.Linq;
using XSpecification.Linq.Pipeline;

public sealed class CustomerSpec : SpecificationBase<Customer, CustomerFilter>
{
    public CustomerSpec(
        ILogger<CustomerSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<Customer> pipeline)
        : base(logger, configuration, pipeline)
    {
        // Filter property is matched to a model property by name automatically.
        // Use HandleField to override or to handle complex cases.
        HandleField(f => f.Name, m => m.FullName);

        // Use OrGroup/AndGroup to query multiple model properties with one filter value.
        OrGroup(f => f.Name, m => m.FullName, m => m.Nickname);
    }
}
```

## Wire up DI

```csharp
services.AddLinqSpecification(cfg =>
{
    cfg.AddSpecification<CustomerSpec>();
});
```

For Elasticsearch backends use `services.AddElasticSpecification(...)` (NEST 7.x) or the equivalent
extension shipped by `XSpecification.Elasticsearch.V8`.

## Use the specification

```csharp
public sealed class CustomerQueries
{
    private readonly CustomerSpec _spec;
    private readonly DbContext _db;

    public CustomerQueries(CustomerSpec spec, DbContext db)
    {
        _spec = spec;
        _db = db;
    }

    public Task<List<Customer>> Search(CustomerFilter filter, CancellationToken ct = default)
    {
        var predicate = _spec.CreateFilterExpression(filter);
        return _db.Set<Customer>().AsExpandable().Where(predicate).ToListAsync(ct);
    }
}
```

For an async-friendly contract, use `CreateFilterExpressionAsync` (LINQ) or
`CreateFilterQueryAsync` (Elasticsearch). They currently delegate to the synchronous implementation
but accept a `CancellationToken` and have a stable signature for future I/O-bound visitors.
