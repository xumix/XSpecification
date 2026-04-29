#nullable disable

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests.Specs;

public class IncompatibleLinqTestSpec : SpecificationBase<LinqTestModel, IncompatibleLinqTestFilter>
{
    /// <inheritdoc />
    public IncompatibleLinqTestSpec(ILogger<IncompatibleLinqTestSpec> logger, SpecificationConfiguration configuration, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        IgnoreField(f => f.Ignored);
        IgnoreField(f => f.NameOrListName);
        IgnoreField(f => f.Conditional);
        HandleField(f => f.Incompatible, m => m.RangeDate);
    }
}
