#nullable disable
using System;

namespace XSpecification.Linq.Tests;

public class LinqTestModel
{
    public int Id { get; set; }

    public int RangeId { get; set; }

    public int ListId { get; set; }

    public string Name { get; set; }

    public string ListName { get; set; }

    public string ComplexName { get; set; }

    public DateTime? NullableDate { get; set; }

    public DateTime Date { get; set; }

    public DateTime ListDate { get; set; }

    public DateTime? RangeDate { get; set; }

    public int UnmatchingProperty { get; set; }
}
