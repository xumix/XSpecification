using Nest;

using XSpecification.Elasticsearch.Abstractions;

namespace XSpecification.Elasticsearch;

/// <summary>
/// NEST 7.x backend that maps the shared <see cref="IQueryNode"/> AST onto NEST's
/// <see cref="QueryContainer"/>.
/// </summary>
public sealed class NestQueryBackend : IQueryBackend<QueryContainer>
{
    /// <summary>Singleton instance.</summary>
    public static NestQueryBackend Instance { get; } = new();

    /// <inheritdoc />
    public QueryContainer MatchAll => new MatchAllQuery();

    /// <inheritdoc />
    public QueryContainer And(QueryContainer left, QueryContainer right) => left && right;

    /// <inheritdoc />
    public QueryContainer Or(QueryContainer left, QueryContainer right) => left || right;

    /// <inheritdoc />
    public QueryContainer Translate(IQueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            MatchAllNode => MatchAll,
            TermNode t => Disable(new TermQuery { Field = t.Field, Value = t.Value }, t.DisableScoring),
            TermsNode ts => Disable(new TermsQuery { Field = ts.Field, Terms = ts.Values }, ts.DisableScoring),
            ExistsNode e => e.Negated
                ? (e.DisableScoring ? +(!new ExistsQuery { Field = e.Field }) : !new ExistsQuery { Field = e.Field })
                : Disable(new ExistsQuery { Field = e.Field }, e.DisableScoring),
            MatchNode m => TranslateMatch(m),
            DateRangeNode dr => TranslateDateRange(dr),
            NumericRangeNode nr => TranslateNumericRange(nr),
            BoolNode b => TranslateBool(b),
            _ => throw new NotSupportedException($"Unsupported query node: {node.GetType().Name}"),
        };
    }

    private QueryContainer TranslateBool(BoolNode b)
    {
        var query = new BoolQuery
        {
            Must = b.Must?.Select(Translate).ToArray(),
            Should = b.Should?.Select(Translate).ToArray(),
            MustNot = b.MustNot?.Select(Translate).ToArray(),
            Filter = b.Filter?.Select(Translate).ToArray(),
        };
        return query;
    }

    private static QueryContainer TranslateMatch(MatchNode m)
    {
        if (string.IsNullOrEmpty(m.Value))
        {
            return new MatchAllQuery();
        }

        QueryBase q = m.Kind switch
        {
            MatchKind.Equal => m.IsFullText
                ? new MatchQuery { Field = m.Field, Query = m.Value }
                : new TermQuery { Field = m.Field, Value = m.Value },
            MatchKind.Contains => new WildcardQuery { Field = m.Field, Value = $"*{m.Value}*" },
            MatchKind.StartsWith => new WildcardQuery { Field = m.Field, Value = $"{m.Value}*" },
            MatchKind.EndsWith => new WildcardQuery { Field = m.Field, Value = $"*{m.Value}" },
            _ => throw new NotSupportedException($"Unsupported match kind: {m.Kind}"),
        };

        return Disable(q, m.DisableScoring);
    }

    private static QueryContainer TranslateDateRange(DateRangeNode dr)
    {
        var query = new DateRangeQuery
        {
            Field = dr.Field,
        };

        if (dr.Start.HasValue)
        {
            if (dr.Exclusive)
            {
                query.GreaterThan = dr.Start.Value.UtcDateTime;
            }
            else
            {
                query.GreaterThanOrEqualTo = dr.Start.Value.UtcDateTime;
            }
        }

        if (dr.End.HasValue)
        {
            if (dr.Exclusive)
            {
                query.LessThan = dr.End.Value.UtcDateTime;
            }
            else
            {
                query.LessThanOrEqualTo = dr.End.Value.UtcDateTime;
            }
        }

        return Disable(query, dr.DisableScoring);
    }

    private static QueryContainer TranslateNumericRange(NumericRangeNode nr)
    {
        var query = new NumericRangeQuery
        {
            Field = nr.Field,
        };

        if (nr.Start.HasValue)
        {
            if (nr.Exclusive)
            {
                query.GreaterThan = nr.Start.Value;
            }
            else
            {
                query.GreaterThanOrEqualTo = nr.Start.Value;
            }
        }

        if (nr.End.HasValue)
        {
            if (nr.Exclusive)
            {
                query.LessThan = nr.End.Value;
            }
            else
            {
                query.LessThanOrEqualTo = nr.End.Value;
            }
        }

        return Disable(query, nr.DisableScoring);
    }

    private static QueryContainer Disable(QueryBase query, bool disable) => disable ? +query : query;
}
