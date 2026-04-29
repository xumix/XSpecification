# Filter primitives

All primitives live in `XSpecification.Core` and map cleanly to both LINQ and Elasticsearch backends.

## `StringFilter`

```csharp
public sealed class CustomerFilter
{
    public StringFilter Name { get; set; } = new();
}

filter.Name.Equal = "Acme";        // exact match (term-level on keyword)
filter.Name.StartsWith = "Acm";    // wildcard / StartsWith
filter.Name.Contains = "cme";      // wildcard / IndexOf
filter.Name.EndsWith = "me";       // wildcard / EndsWith
```

## `RangeFilter<T>` (struct types)

```csharp
public RangeFilter<DateTime> CreatedAt { get; set; } = new();

filter.CreatedAt.Start = DateTime.UtcNow.AddDays(-7);
filter.CreatedAt.End = DateTime.UtcNow;
filter.CreatedAt.IsExclusive = false;        // gte / lte
filter.CreatedAt.UseStartAsEquals = false;   // when true: == start, ignore end
filter.CreatedAt.IsNotNull = true;           // also assert non-null
```

## `ListFilter<T>`

```csharp
public ListFilter<int> StatusIn { get; set; } = new();
filter.StatusIn.Values = [1, 2, 3];

// or via the implicit constructor
filter.StatusIn = new[] { 1, 2, 3 };
```

## `BoolFilter` (new in 2.0)

`BoolFilter` is a tri-state filter — *unset*, `true`, or `false`. Use it when the model field is a
plain `bool` and you still want callers to opt out of filtering it.

```csharp
public BoolFilter IsVip { get; set; } = new();
filter.IsVip = true;             // implicit conversion
filter.IsVip.HasValue();         // true
filter.IsVip = new BoolFilter(); // unset — no filtering
```

## `INullableFilter`

Every nullable-aware filter primitive supports `IsNull` / `IsNotNull` (mutually exclusive). When
both are unset, no null/not-null clause is emitted.
