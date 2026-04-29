using System.Linq.Expressions;
using System.Reflection;

namespace XSpecification.Core.Pipeline;

public abstract class Context
{
    public PropertyInfo? FilterProperty { get; set; }

    public object? FilterPropertyValue { get; set; }

    public PropertyInfo? ModelProperty { get; set; }

    public LambdaExpression? ModelPropertyExpression { get; set; }
}
