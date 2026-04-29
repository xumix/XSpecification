namespace XSpecification.Elasticsearch.Abstractions;

/// <summary>Match kind for textual filters.</summary>
public enum MatchKind
{
    /// <summary>Exact match (term-level for keyword fields, match for analysed fields).</summary>
    Equal,

    /// <summary>Substring match: <c>*value*</c>.</summary>
    Contains,

    /// <summary>Prefix match: <c>value*</c>.</summary>
    StartsWith,

    /// <summary>Suffix match: <c>*value</c>.</summary>
    EndsWith,
}

/// <summary>Equivalent of Elasticsearch <c>term</c> query.</summary>
public sealed record TermNode(string Field, object? Value, bool DisableScoring = true) : IQueryNode;

/// <summary>Equivalent of Elasticsearch <c>terms</c> query (set membership).</summary>
public sealed record TermsNode(string Field, IReadOnlyList<object?> Values, bool DisableScoring = true) : IQueryNode;

/// <summary>Text / keyword match. Maps to <c>match</c>, <c>wildcard</c> or <c>term</c> depending on field type and <see cref="Kind"/>.</summary>
public sealed record MatchNode(string Field, string? Value, MatchKind Kind, bool IsFullText, bool DisableScoring = true) : IQueryNode;

/// <summary>Equivalent of Elasticsearch <c>date_range</c> query.</summary>
public sealed record DateRangeNode(
    string Field,
    DateTimeOffset? Start,
    DateTimeOffset? End,
    bool Exclusive = false,
    bool DisableScoring = true) : IQueryNode;

/// <summary>Equivalent of Elasticsearch numeric <c>range</c> query.</summary>
public sealed record NumericRangeNode(
    string Field,
    double? Start,
    double? End,
    bool Exclusive = false,
    bool DisableScoring = true) : IQueryNode;

/// <summary>
/// Equivalent of Elasticsearch <c>exists</c> query (or <c>must_not exists</c> when <see cref="Negated"/> is true).
/// </summary>
public sealed record ExistsNode(string Field, bool Negated = false, bool DisableScoring = true) : IQueryNode;

/// <summary>Boolean composition. Each list is processed as the corresponding clause in <c>bool</c>.</summary>
public sealed record BoolNode(
    IReadOnlyList<IQueryNode>? Must = null,
    IReadOnlyList<IQueryNode>? Should = null,
    IReadOnlyList<IQueryNode>? MustNot = null,
    IReadOnlyList<IQueryNode>? Filter = null) : IQueryNode;

/// <summary>
/// "True" placeholder used as the identity element when combining many partial nodes.
/// Backends should map it to <c>match_all</c>.
/// </summary>
public sealed record MatchAllNode : IQueryNode
{
    public static MatchAllNode Instance { get; } = new();
}
