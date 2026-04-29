#nullable disable

using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nest;

using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Elastic.Tests.Specs;

public class LinqTestSpec : SpecificationBase<ElasticTestModel, ElsaticTestFilter>
{
    /// <inheritdoc />
    public LinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
        IgnoreField(f => f.Ignored);
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        HandleField(f => f.Conditional, (prop, filter) =>
        {
            if (filter.Conditional)
            {
                return CreateQueryFromFilterProperty(prop, f => f.Name, filter.Conditional.ToString());
            }

            if (!filter.Conditional && filter.Id == 312)
            {
                var q1 = new TermQuery
                {
                    Field = GetIndexFieldName(f => f.Date),
                    Value = DateTime.Today
                };

                var q2 = new TermQuery
                {
                    Field = GetIndexFieldName(f => f.UnmatchingProperty),
                    Value = 123
                };
                return q1 && q2;
            }

            return null;
        });
        HandleField(f => f.NameOrListName, (prop, filter) =>
        {
            var ne = CreateQueryFromFilterProperty(prop, f => f.Name, filter.NameOrListName);
            var le = CreateQueryFromFilterProperty(prop, f => f.ListName, filter.NameOrListName);

            if (ne != null && le != null)
            {
                return ne || le;
            }

            return ne ?? le;
        });
    }
}
