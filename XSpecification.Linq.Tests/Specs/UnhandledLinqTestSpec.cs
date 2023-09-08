#nullable disable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Linq.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Linq.Tests.Specs;

public class UnhandledLinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
{
    /// <inheritdoc />
    public UnhandledLinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
    }
}
