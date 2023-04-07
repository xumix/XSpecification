#nullable disable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests;

public class IncompatibleLinqTestSpec : SpecificationBase<LinqTestModel, IncompatibleLinqTestFilter>
{
    /// <inheritdoc />
    public IncompatibleLinqTestSpec(ILogger<IncompatibleLinqTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        HandleField(f => f.Incompatible, m => m.RangeDate);
    }
}
