namespace XSpecification.Linq;

public interface IFilterConverter
{
    void CreateExpression<TModel>(
        ExpressionCreationContext<TModel> context,
        Action<ExpressionCreationContext<TModel>> next);
}
