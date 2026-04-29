#nullable disable

using Microsoft.Extensions.Logging;

using XSpecification.Core;
using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elastic.Tests.Specs;

public class IncompatibleLinqTestSpec : SpecificationBase<ElasticTestModel, IncompatibleElasticTestFilter>
{
    /// <inheritdoc />
    public IncompatibleLinqTestSpec(ILogger<IncompatibleLinqTestSpec> logger, SpecificationConfiguration configuration, IFilterHandlerPipeline handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        IgnoreField(f => f.Ignored);
        IgnoreField(f => f.NameOrListName);
        IgnoreField(f => f.Conditional);
        HandleField(f => f.Incompatible, m => m.RangeDate);
    }
}
