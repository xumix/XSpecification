#nullable disable
using System;

using XSpecification.Core;

namespace XSpecification.Linq.Tests;

public class LinqTestFilter
{
    public int? Id { get; set; }

    public RangeFilter<int> RangeId { get; set; }

    public ListFilter<int> ListId { get; set; }

    public string Name { get; set; }

    public ListFilter<string> ListName { get; set; }

    public StringFilter ComplexName { get; set; }

    public DateTime? NullableDate { get; set; }

    public DateTime? Date { get; set; }

    public ListFilter<DateTime> ListDate { get; set; }

    public RangeFilter<DateTime> RangeDate { get; set; }

    public ListFilter<int> Explicit { get; set; }

    public bool Conditional { get; set; }

    public string Ignored { get; set; }

    public string NameOrListName { get; set; }
}
