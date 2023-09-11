using System.Linq.Expressions;

using Elasticsearch.Net;

using Nest;

using Newtonsoft.Json.Serialization;

namespace XSpecification.Elasticsearch;

public static class QueryHelpers
{
    public static string GetPropertyPath<T, TProp>(Expression<Func<T, TProp>> indexPath, NamingStrategy namingStrategy)
    {
        try
        {
            var paths = indexPath.GetPropertyPathParts()!.Select(p => namingStrategy.GetPropertyName(p, false));
            var propName = string.Join(".", paths);
            return propName;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Unable get index property name from indexPath: {indexPath}", e);
        }
    }

    public static string[]? GetPropertyPathParts<T, TResult>(this Expression<Func<T, TResult>>? selector)
    {
        return selector?.GetPropertyExpressionPath()?.Select(s => s.Member.Name).ToArray();
    }

    public static MemberExpression[]? GetPropertyExpressionPath(this Expression? selector)
    {
        return selector == null
            ? (MemberExpression[]?)null
            : new ExpressionMemberVisitor(selector).Expressions.ToArray();
    }

    /// <summary>
    /// Prints query into string.
    /// </summary>
    /// <param name="self">The self.</param>
    /// <returns>The value.</returns>
    public static string ToPrettyString(this QueryContainer self)
    {
        using (var settings = new ConnectionSettings().EnableDebugMode())
        {
            var client = new ElasticClient(settings);
            return client.RequestResponseSerializer.SerializeToString(self);
        }
    }
}
