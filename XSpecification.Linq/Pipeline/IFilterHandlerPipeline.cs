namespace XSpecification.Linq.Pipeline;

public interface IFilterHandlerPipeline<TModel>
{
    /// <summary>
    /// Invoke the pipeline to the end, or until an exception is thrown.
    /// </summary>
    /// <param name="context">The creation context.</param>
    void Execute(LinqFilterContext<TModel> context);
}
