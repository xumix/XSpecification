using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

namespace XSpecification.Linq.Pipeline;

public abstract class Context
{
    public PropertyInfo? FilterProperty { get; set; }

    public object? FilterPropertyValue { get; set; }

    public PropertyInfo? ModelProperty { get; set; }

    public LambdaExpression? ModelPropertyExpression { get; set; }
}

public class Context<TModel> : Context
{
    public Context()
    {
        Expression = PredicateBuilder.New<TModel>(true);
    }

    public ExpressionStarter<TModel> Expression { get; }
}
