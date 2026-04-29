using System.Linq.Expressions;

using LinqKit;

namespace XSpecification.Linq;

public static class KnownExpressions<TModel>
{
    public static readonly Expression<Func<TModel, bool>> AlwaysFalseExpression = a => false;

    public static readonly Expression<Func<TModel, bool>> DoNothing =
        PredicateBuilder.New<TModel>(true).DefaultExpression;
}
