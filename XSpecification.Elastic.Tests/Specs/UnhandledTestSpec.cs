#nullable disable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Elastic.Tests.Specs;

public class UnhandledTestSpec : SpecificationBase<ElasticTestModel, ElsaticTestFilter>
{
    /// <inheritdoc />
    public UnhandledTestSpec(ILogger<UnhandledTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
    }
}
