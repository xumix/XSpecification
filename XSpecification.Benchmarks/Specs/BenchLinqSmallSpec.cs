using Microsoft.Extensions.Logging;

using XSpecification.Benchmarks.Models;
using XSpecification.Core;
using XSpecification.Linq;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Benchmarks.Specs;

public class BenchLinqSmallSpec : SpecificationBase<BenchModel, BenchSmallFilter>
{
    public BenchLinqSmallSpec(
        ILogger<BenchLinqSmallSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<BenchModel> handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
    }
}

public class BenchLinqLargeSpec : SpecificationBase<BenchModel, BenchLargeFilter>
{
    public BenchLinqLargeSpec(
        ILogger<BenchLinqLargeSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<BenchModel> handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
        HandleField(f => f.IdIn, m => m.Id);
        HandleField(f => f.NameIn, m => m.Name);
    }
}
