namespace XSpecification.Elasticsearch.Abstractions;

/// <summary>
/// Marker interface for the backend-agnostic query AST that XSpecification specifications produce.
/// Each backend (NEST 7.x, Elastic.Clients.Elasticsearch 8.x) provides a translator that maps
/// these nodes onto its native query model.
/// </summary>
public interface IQueryNode
{
}
