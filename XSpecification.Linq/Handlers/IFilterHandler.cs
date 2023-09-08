using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Handlers;

public interface IFilterHandler
{
    void Handle<TModel>(
        LinqFilterContext<TModel> context,
        Action<LinqFilterContext<TModel>> next);

    bool CanHandle<TModel>(LinqFilterContext<TModel> context);
}
