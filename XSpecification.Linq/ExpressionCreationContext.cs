using System.Linq.Expressions;
using System.Reflection;

using LinqKit;

namespace XSpecification.Linq;

public abstract class ExpressionCreationContext
{
    public PropertyInfo? FilterProperty { get; set; }

    public object? FilterPropertyValue { get; set; }

    public PropertyInfo? ModelProperty { get; set; }

    public LambdaExpression? ModelPropertyExpression { get; set; }
}

public class ExpressionCreationContext<TModel> : ExpressionCreationContext
{
    public ExpressionCreationContext()
    {
        Expression = PredicateBuilder.New<TModel>(true);
    }

    public ExpressionStarter<TModel> Expression { get; }
}
