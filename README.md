# XSpecification is an implementation of Specification pattern for Linq and Elasticsearch
Implemented filters: **RangeFilter** (BETWEEN x AND y), **ListFilter** (IN(x,y,z)), **StringFilter** (=, LIKE '%x%'), direct comparison, NULL/NOT NULL

This library helps with removing boilerplate code and adds commonly used filtering capabilities, especially if your app has many grids with similar filtering capabilities.
It could be useful in BL-heavy scenarios when you find yourself writing code like this:
```
public class LinqTestFilter
{
    public DateTime? Date { get;set }
    public string Name { get;set }
    public string NameContains { get;set }
    public int? IdFrom { get; set; }
    public int? IdTo { get; set; }
}

var filter = new LinqTestFilter
    {
        Date = DateTime.Today,
        NameContains = "complex",
        IdFrom = 0,
        IdTo = 5
    };

var where = PredicateBuilder.New<LinqTestModel>();
if (filter.Date.HasValue)
{
    where.And(f => f.Date == filter.Date.Value);
}
if (!string.IsNullOrEmpty(filter.Name))
{
    where.And(f => f.Name == filter.Name);
}
if (!string.IsNullOrEmpty(filter.NameContains))
{
    where.And(f => f.ComplexName.Contains(filter.ComplexName.Value));
}
if (filter.IdFrom.HasValue())
{
    where.And(f => f.Id >= filter.IdFrom);
}
if (filter.IdTo.HasValue())
{
    where.And(f => f.Id <= filter.IdTo);
}
    
var data = dbcontext.Set<LinqTestModel>().Where(where);
```


# Setup
Formalize your filter
```Csharp
public class LinqTestFilter
{
   public int Id { get; set; }
   public RangeFilter<int> RangeId { get; set; }
}
```
Create your specification, if a filter property has the same name as in DB model **it will be mapped automatically**
More examples in the test project.

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
// add specification to your DI
services.AddSingleton<LinqTestSpec>();
```

# Using in your code
```Csharp
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
