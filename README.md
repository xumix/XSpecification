# XSpecification is an implementation of Specification pattern for Linq and Elasticsearch

# Setup
Formalize your filter
```Csharp
public class LinqTestFilter
{
   public int Id { get; set; }
   public RangeFilter<int> RangeId { get; set; }
}
```
Create your scpecification
```Csharp
public class LinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
{
    public LinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options)
        : base(logger, options)
    {
        IgnoreField(f => f.Ignored);
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        HandleField(f => f.Conditional, (prop, filter) =>
        {
            if (filter.Conditional)
            {
                return CreateExpressionFromFilterProperty(prop, f => f.Name, filter.Conditional.ToString());
            }

            if(!filter.Conditional && filter.Id == 312)
            {
                return PredicateBuilder.New<LinqTestModel>()
                                       .And(f => f.Date.Hour == 1)
                                       .And(f => f.UnmatchingProperty == 123);
            }

            return DoNothing;
        });
    }
}
```

Add XSpecification to your DI in Startup.cs or Program.cs
```Csharp
services.AddLinqSpecification();
//or optionally disable auto property handling
services.AddLinqSpecification(o =>
            {
                o.DisableAutoPropertyHandling = true;
            });
// add scpecification to your DI
services.AddSingleton<LinqTestSpec>();
```

# Using in your code
```Csharp
//Before:
var filter = new LinqTestFilter
{
    Date = DateTime.Today,
    ComplexName = new StringFilter("complex") { Contains = true },
    RangeId = new RangeFilter<int> { Start = 0, End = 5 }
};

var where = PredicateBuilder.New<LinqTestModel>();

if (filter.Date.HasValue)
{
    where.And(f => f.Date == filter.Date.Value);
}
if (filter.ComplexName != null)
{
    where.And(f => f.ComplexName.Contains(filter.ComplexName.Value));
}
if (filter.RangeId.HasValue())
{
    where.And(f => f.Id >= filter.RangeId.Start && f.Id <= filter.RangeId.End);
}

dbcontext.Set<LinqTestModel>().Where(where);


//After
// Inject from DI
var spec = serviceProvider.GetRequiredService<LinqTestSpec>();

var filter = new LinqTestFilter
{
    Date = DateTime.Today,
    Id = 123,
    Name = "qwe",
    ComplexName = new StringFilter("complex") { Contains = true },
    ListDate = new[] { DateTime.Today, DateTime.Today.AddDays(1) },
    ListId = new[] { 1, 2, 3 },
    ListName = new[] { "a", "b", "z" },
    NullableDate = DateTime.Today.AddDays(-1),
    RangeDate = new RangeFilter<DateTime> { Start = DateTime.Today, End = DateTime.Today.AddDays(1) },
    RangeId = new RangeFilter<int> { Start = 0, End = 5 }
};

var expression = spec.CreateFilterExpression(filter);

var data = dbcontext.Set<LinqTestModel>().Where(expression);

```

# TODO
* Add extension points for new filter types
* Refactor Expression creation logic to Chain of responsipility
* Add Elasticsearch implementation
