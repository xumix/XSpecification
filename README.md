# XSpecification is an implementation of Specification pattern for Linq and Elasticsearch
Implemented filters: **RangeFilter** (BETWEEN x AND y), **ListFilter** (IN(x,y,z)), **StringFilter** (=, LIKE '%x%'), direct comparison, NULL/NOT NULL

This library helps with removing boilerplate code and adds commonly used filtering capabilities, especially if your app has many grids with similar filtering capabilities.
It could be useful in BL-heavy scenarios when you find yourself writing code like this:
```Csharp
public class SomeApiFilter
{
    public DateTime? Date { get;set }
    public string Name { get;set }
    public string NameContains { get;set }
    public int? IdFrom { get; set; }
    public int? IdTo { get; set; }
}

var filter = new SomeApiFilter
    {
        Date = DateTime.Today,
        NameContains = "complex",
        IdFrom = 0,
        IdTo = 5
    };

var where = PredicateBuilder.New<DbModel>();
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
    where.And(f => f.Name.Contains(filter.NameContains));
}
if (filter.IdFrom.HasValue)
{
    where.And(f => f.Id >= filter.IdFrom);
}
if (filter.IdTo.HasValue)
{
    where.And(f => f.Id <= filter.IdTo);
}

var data = dbcontext.Set<DbModel>().Where(where);
```
With XSpecification it becomes this:
```Csharp
var expression = spec.CreateFilterExpression(filter);
var data = dbcontext.Set<DbModel>().Where(expression);
```


# Setup
Formalize your filter
```Csharp
public class LinqTestFilter
{
   public int Id { get; set; }
   public StringFilter Name { get; set; }
   public string Explicit { get; set; }
   public bool Conditional { get; set; }
}
```
Create your specification, if a filter property has the same name as in DB model **it will be mapped automatically**.
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
//or optionally disable convention-based property handling
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
    Date = DateTime.Today, // Date = 'xxxx-xx-xx....'
    Id = 123, // Id = 123
    Name = "qwe", // Name = 'qwe'
    ComplexName = new StringFilter("complex") { Contains = true }, // ComplexName LIKE '%complex%'
    ListDate = new[] { DateTime.Today, DateTime.Today.AddDays(1) }, // ListDate IN ('xxxx-xx-xx....', 'xxxx-xx-xx....')
    ListId = new[] { 1, 2, 3 }, // ListId IN (1, 2, 3)
    ListName = new ListFilter<string>("a", "b", "z") { IsInverted = true }, // ListName NOT IN ('a', 'b', 'b')
    NullableDate = DateTime.Today.AddDays(-1), // NullableDate = 'xxxx-xx-xx....'
    RangeDate = new RangeFilter<DateTime> { Start = DateTime.Today, End = DateTime.Today.AddDays(1) }, // RangeDate >= 'xxxx-xx-xx....' AND RangeDate <= 'yyyy-yy-yy....'
    RangeId = new RangeFilter<int> { Start = 0, End = 5 } // RangeId >= 0 AND RangeId <= 5
};

var expression = spec.CreateFilterExpression(filter);
var data = dbcontext.Set<LinqTestModel>().Where(expression);

/* will be translated to
SELECT ... FROM LinqTestModel
WHERE Date = 'xxxx-xx-xx....' AND Id = 123 AND ComplexName LIKE '%complex%' AND ListDate IN ('xxxx-xx-xx....', 'xxxx-xx-xx....')
AND ListId IN (1, 2, 3) AND ListName NOT IN ('a', 'b', 'b') AND NullableDate = 'xxxx-xx-xx....'
AND RangeDate >= 'xxxx-xx-xx....' AND RangeDate <= 'yyyy-yy-yy....' AND RangeId >= 0 AND RangeId <= 5
*/

```

# TODO
* Add extension points for new filter types
* Refactor Expression creation logic to Chain of responsipility
* Add Elasticsearch implementation
