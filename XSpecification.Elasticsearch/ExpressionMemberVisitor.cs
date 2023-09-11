using System.Linq.Expressions;

namespace XSpecification.Elasticsearch;

public sealed class ExpressionMemberVisitor : ExpressionVisitor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="T:Pp3.Common.Expressions.ExpressionCollectionVisitor" /> class.
    /// </summary>
    /// <param name="expression">
    ///     The expression tree to walk when populating this collection.
    /// </param>
    public ExpressionMemberVisitor(Expression expression) => Visit(expression);

    public List<MemberExpression> Expressions { get; } = new();

    /// <summary>
    ///     Processes the provided <see cref="T:System.Linq.Expressions.Expression" /> object by adding it to this collection
    ///     and then walking further down the tree.
    /// </summary>
    /// <param name="node">
    ///     The expression to process to add to this collection and walk through.
    /// </param>
    /// <returns>
    ///     The modified expression, assuming the expression was modified; otherwise, returns the
    ///     original expression.
    /// </returns>
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is MemberExpression expression)
        {
            VisitMember(expression);
            Expressions.Add(node);
            return node;
        }

        Expressions.Add(node);
        return base.VisitMember(node);
    }
}
