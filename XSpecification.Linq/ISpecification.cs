using System.Linq.Expressions;

namespace XSpecification.Linq;

public interface ISpecification
{
    LambdaExpression CreateFilterExpression(object filter);
}
