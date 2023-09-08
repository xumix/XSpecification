using LinqKit;

using XSpecification.Core;
using XSpecification.Core.Pipeline;

namespace XSpecification.Linq.Pipeline;

public class LinqFilterContext<TModel> : Context
{
    public LinqFilterContext()
    {
        Expression = PredicateBuilder.New<TModel>(true);
    }

    public ExpressionStarter<TModel> Expression { get; }
}
