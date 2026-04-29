#nullable disable
using XSpecification.Core;

namespace XSpecification.Elastic.Tests;

public class IncompatibleElasticTestFilter : ElasticTestFilter
{
    public ListFilter<int> Incompatible { get; set; }
}
