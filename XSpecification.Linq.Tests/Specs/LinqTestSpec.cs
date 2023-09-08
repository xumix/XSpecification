#nullable disable

using LinqKit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using XSpecification.Linq.Pipeline;

using Options = XSpecification.Core.Options;

namespace XSpecification.Linq.Tests.Specs;

public class LinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
{
    /// <inheritdoc />
    public LinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options, IFilterHandlerPipeline<LinqTestModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
        IgnoreField(f => f.Ignored);
        HandleField(f => f.Explicit, m => m.UnmatchingProperty);
        HandleField(f => f.Conditional, (prop, filter) =>
        {
            if (filter.Conditional)
            {
                return CreateExpressionFromFilterProperty(prop, f => f.Name, filter.Conditional.ToString());
            }

            if (!filter.Conditional && filter.Id == 312)
            {
                return PredicateBuilder.New<LinqTestModel>()
                                       .And(f => f.Date.Hour == 1)
                                       .And(f => f.UnmatchingProperty == 123);
            }

            return null;
        });
        HandleField(f => f.NameOrListName, (prop, filter) =>
        {
            var ne = CreateExpressionFromFilterProperty(prop, f => f.Name, filter.NameOrListName);
            var le = CreateExpressionFromFilterProperty(prop, f => f.ListName, filter.NameOrListName);

            if (ne != null && le != null)
            {
                return ne.Or(le);
            }

            return ne ?? le;
        });
    }
}
