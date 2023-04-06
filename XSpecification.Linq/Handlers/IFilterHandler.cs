using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public interface IFilterHandler
{
    void CreateExpression<TModel>(
        Context<TModel> context,
        Action<Context<TModel>> next);

    bool CanHandle<TModel>(Context<TModel> context);
}
