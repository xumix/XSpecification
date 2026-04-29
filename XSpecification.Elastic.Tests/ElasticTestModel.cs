#nullable disable
using System;

using Nest;

namespace XSpecification.Elastic.Tests;

public class ElasticTestModel
{
    [Number(NumberType.Integer)]
    public int Id { get; set; }

    [Number(NumberType.Integer)]
    public int RangeId { get; set; }

    [Number(NumberType.Integer)]
    public int ListId { get; set; }

    [Keyword]
    public string Name { get; set; }

    [Keyword]
    public string ListName { get; set; }

    [Keyword]
    public string ComplexName { get; set; }

    [Date]
    public DateTime? NullableDate { get; set; }

    [Date]
    public DateTime Date { get; set; }

    [Date]
    public DateTime ListDate { get; set; }

    [Date]
    public DateTime? RangeDate { get; set; }

    [Number(NumberType.Integer, Boost = 1)]
    public int UnmatchingProperty { get; set; }
}
