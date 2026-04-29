#nullable disable

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elastic.Tests.Specs;

public class UnhandledTestSpec : SpecificationBase<ElasticTestModel, ElasticTestFilter>
{
    /// <inheritdoc />
    public UnhandledTestSpec(ILogger<UnhandledTestSpec> logger, SpecificationConfiguration configuration, IFilterHandlerPipeline handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
    }
}
