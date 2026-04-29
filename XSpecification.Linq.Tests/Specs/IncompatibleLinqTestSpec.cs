#nullable disable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Linq.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Linq.Tests.Specs;

public class IncompatibleLinqTestSpec : SpecificationBase<LinqTestModel, IncompatibleLinqTestFilter>
{
    /// <inheritdoc />
    public IncompatibleLinqTestSpec(ILogger<IncompatibleLinqTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        IgnoreField(f => f.Ignored);
        IgnoreField(f => f.NameOrListName);
        IgnoreField(f => f.Conditional);
        HandleField(f => f.Incompatible, m => m.RangeDate);
    }
}
