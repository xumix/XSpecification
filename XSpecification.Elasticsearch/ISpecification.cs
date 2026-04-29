using Nest;

namespace XSpecification.Elasticsearch;

public interface ISpecification
{
    QueryContainer CreateFilterQuery(object filter);
}
