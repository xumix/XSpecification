#nullable disable
using XSpecification.Core;

namespace XSpecification.Elastic.Tests;

public class IncompatibleElsaticTestFilter : ElsaticTestFilter
{
    public ListFilter<int> Incompatible { get; set; }
}
