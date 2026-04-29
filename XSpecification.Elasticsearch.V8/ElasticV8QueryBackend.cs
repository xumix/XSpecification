using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

using XSpecification.Elasticsearch.Abstractions;

namespace XSpecification.Elasticsearch.V8;

/// <summary>
/// Elastic.Clients.Elasticsearch 8.x backend that maps the shared <see cref="IQueryNode"/> AST onto
/// <see cref="Query"/>.
/// </summary>
public sealed class ElasticV8QueryBackend : IQueryBackend<Query>
{
    /// <summary>Singleton instance.</summary>
    public static ElasticV8QueryBackend Instance { get; } = new();

    /// <inheritdoc />
    public Query MatchAll => new MatchAllQuery();

    /// <inheritdoc />
    public Query And(Query left, Query right) => left & right;

    /// <inheritdoc />
    public Query Or(Query left, Query right) => left | right;

    /// <inheritdoc />
    public Query Translate(IQueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return node switch
        {
            MatchAllNode => MatchAll,
            TermNode t => new TermQuery { Field = t.Field, Value = ToFieldValue(t.Value) },
            TermsNode ts => new TermsQuery
            {
                Field = ts.Field,
                Terms = new TermsQueryField(ts.Values
                    .Select(v => ToFieldValue(v))
                    .ToArray()),
            },
            ExistsNode { Negated: true } e => new BoolQuery { MustNot = [new ExistsQuery { Field = e.Field }] },
            ExistsNode e => new ExistsQuery { Field = e.Field },
            MatchNode m => TranslateMatch(m),
            DateRangeNode dr => TranslateDateRange(dr),
            NumericRangeNode nr => TranslateNumericRange(nr),
            BoolNode b => TranslateBool(b),
            _ => throw new NotSupportedException($"Unsupported query node: {node.GetType().Name}"),
        };
    }

    private Query TranslateBool(BoolNode b)
    {
        return new BoolQuery
        {
            Must = b.Must?.Select(Translate).ToArray(),
            Should = b.Should?.Select(Translate).ToArray(),
            MustNot = b.MustNot?.Select(Translate).ToArray(),
            Filter = b.Filter?.Select(Translate).ToArray(),
        };
    }

    private static Query TranslateMatch(MatchNode m)
    {
        if (string.IsNullOrEmpty(m.Value))
        {
            return new MatchAllQuery();
        }

        return m.Kind switch
        {
            MatchKind.Equal when m.IsFullText => new MatchQuery { Field = m.Field, Query = m.Value },
            MatchKind.Equal => new TermQuery { Field = m.Field, Value = m.Value },
            MatchKind.Contains => new WildcardQuery(m.Field) { Value = $"*{m.Value}*" },
            MatchKind.StartsWith => new WildcardQuery(m.Field) { Value = $"{m.Value}*" },
            MatchKind.EndsWith => new WildcardQuery(m.Field) { Value = $"*{m.Value}" },
            _ => throw new NotSupportedException($"Unsupported match kind: {m.Kind}"),
        };
    }

    private static Query TranslateDateRange(DateRangeNode dr)
    {
        var query = new DateRangeQuery(dr.Field);

        if (dr.Start.HasValue)
        {
            if (dr.Exclusive)
            {
                query.Gt = dr.Start.Value.UtcDateTime;
            }
            else
            {
                query.Gte = dr.Start.Value.UtcDateTime;
            }
        }

        if (dr.End.HasValue)
        {
            if (dr.Exclusive)
            {
                query.Lt = dr.End.Value.UtcDateTime;
            }
            else
            {
                query.Lte = dr.End.Value.UtcDateTime;
            }
        }

        return query;
    }

    private static Query TranslateNumericRange(NumericRangeNode nr)
    {
        var query = new NumberRangeQuery(nr.Field);

        if (nr.Start.HasValue)
        {
            if (nr.Exclusive)
            {
                query.Gt = nr.Start.Value;
            }
            else
            {
                query.Gte = nr.Start.Value;
            }
        }

        if (nr.End.HasValue)
        {
            if (nr.Exclusive)
            {
                query.Lt = nr.End.Value;
            }
            else
            {
                query.Lte = nr.End.Value;
            }
        }

        return query;
    }

    private static FieldValue ToFieldValue(object? value) => value switch
    {
        null => FieldValue.Null,
        bool b => b,
        string s => s,
        long l => l,
        int i => i,
        double d => d,
        float f => f,
        _ => FieldValue.String(value.ToString() ?? string.Empty),
    };
}
