# LINQ backend

`XSpecification.Linq` produces `Expression<Func<TModel, bool>>` predicates that can be applied to
any `IQueryable<T>` (EF Core, in-memory `IEnumerable`, NHibernate, etc.). The output is optimised
through `Linq.Expression.Optimizer` to avoid trivially-true / trivially-false sub-expressions.

## Registering

```csharp
services.AddLinqSpecification(cfg =>
{
    cfg.AddSpecification<CustomerSpec>();
});

// Optional second parameter: tweak SpecificationConfiguration.
services.AddLinqSpecification(
    configureAction: cfg => cfg.AddSpecification<CustomerSpec>(),
    configure: c => c.DisablePropertyAutoHandling = true);
```

## Building a predicate

```csharp
var predicate = spec.CreateFilterExpression(filter);
var customers = await dbContext.Customers
    .AsExpandable()
    .Where(predicate)
    .ToListAsync();
```

`AsExpandable()` from LinqKit is required when the predicate uses `PredicateBuilder.New<T>()`
(which the library does internally to combine sub-expressions).

## Async surface

```csharp
var predicate = await spec.CreateFilterExpressionAsync(filter, cancellationToken);
```

The async overload currently delegates to the synchronous implementation; the contract is stable
for future I/O-bound visitors.
