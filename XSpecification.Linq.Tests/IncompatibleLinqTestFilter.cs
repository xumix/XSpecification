#nullable disable
using XSpecification.Core;

namespace XSpecification.Linq.Tests;

public class IncompatibleLinqTestFilter : LinqTestFilter
{
    public ListFilter<int> Incompatible { get; set; }
}
