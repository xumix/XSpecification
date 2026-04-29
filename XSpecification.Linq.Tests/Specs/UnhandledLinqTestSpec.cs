#nullable disable

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests.Specs;

public class UnhandledLinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
{
    /// <inheritdoc />
    public UnhandledLinqTestSpec(ILogger<LinqTestSpec> logger, SpecificationConfiguration configuration, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
    }
}
