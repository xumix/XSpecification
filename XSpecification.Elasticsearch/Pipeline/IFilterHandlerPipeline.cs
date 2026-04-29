namespace XSpecification.Elasticsearch.Pipeline;

public interface IFilterHandlerPipeline
{
    /// <summary>
    /// Invoke the pipeline to the end, or until an exception is thrown.
    /// </summary>
    /// <param name="context">The creation context.</param>
    void Execute(QueryContext context);
}
