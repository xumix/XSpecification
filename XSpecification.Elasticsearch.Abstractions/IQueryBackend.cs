namespace XSpecification.Elasticsearch.Abstractions;

/// <summary>
/// Translator from the shared <see cref="IQueryNode"/> AST to a backend-specific query type
/// (e.g. <c>Nest.QueryContainer</c>, <c>Elastic.Clients.Elasticsearch.QueryDsl.Query</c>).
/// </summary>
/// <typeparam name="TQuery">Backend-specific query type.</typeparam>
public interface IQueryBackend<TQuery>
{
    /// <summary>Identity element used when no filter is active (typically <c>match_all</c>).</summary>
    TQuery MatchAll { get; }

    /// <summary>Translate a single AST node into a backend query.</summary>
    TQuery Translate(IQueryNode node);

    /// <summary>Combine partial backend queries with logical AND (<c>must</c>).</summary>
    TQuery And(TQuery left, TQuery right);

    /// <summary>Combine partial backend queries with logical OR (<c>should</c>).</summary>
    TQuery Or(TQuery left, TQuery right);
}
