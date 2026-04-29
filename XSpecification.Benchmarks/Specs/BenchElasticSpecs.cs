using Microsoft.Extensions.Logging;

using XSpecification.Benchmarks.Models;
using XSpecification.Core;
using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Benchmarks.Specs;

public class BenchElasticSmallSpec : SpecificationBase<BenchModel, BenchSmallFilter>
{
    public BenchElasticSmallSpec(
        ILogger<BenchElasticSmallSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
    }
}

public class BenchElasticLargeSpec : SpecificationBase<BenchModel, BenchLargeFilter>
{
    public BenchElasticLargeSpec(
        ILogger<BenchElasticLargeSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
        HandleField(f => f.IdIn, m => m.Id);
        HandleField(f => f.NameIn, m => m.Name);
    }
}
