using XSpecification.Core;

namespace XSpecification.Benchmarks.Models;

public class BenchModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}

public class BenchSmallFilter
{
    public int? Id { get; set; }

    public StringFilter? Name { get; set; }
}

public class BenchLargeFilter
{
    public int? Id { get; set; }

    public StringFilter? Name { get; set; }

    public RangeFilter<DateTime>? Date { get; set; }

    public RangeFilter<int>? Quantity { get; set; }

    public RangeFilter<decimal>? Price { get; set; }

    public ListFilter<int>? IdIn { get; set; }

    public ListFilter<string>? NameIn { get; set; }

    public bool? IsActive { get; set; }
}
