#nullable disable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests;

public class UnhandledLinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
{
    /// <inheritdoc />
    public UnhandledLinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
    }
}
