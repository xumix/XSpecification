using System.Linq.Expressions;

using XSpecification.Core;

namespace XSpecification.Linq;

public class NullableFilterConverter : IFilterConverter
{
    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(
        ExpressionCreationContext<TModel> context,
        Action<ExpressionCreationContext<TModel>> next)
    {
        if (context.FilterProperty == null ||
            !typeof(INullableFilter).IsAssignableFrom(context.FilterProperty.PropertyType))
        {
            next(context);
            return;
        }

        var value = (INullableFilter?)context.FilterPropertyValue;
        var ret = GetNullableExpression<TModel, INullableFilter>(context.ModelProperty, value);
        if (ret != default)
        {
            context.Expression.And(ret);
        }
    }

    protected static Expression<Func<TModel, bool>>? GetNullableExpression<TModel, TProperty>(
        Expression<Func<TModel, TProperty>> prop,
        INullableFilter value)
    {
        if (!typeof(TProperty).IsNullable())
        {
            return value.IsNull ? KnownExpressions<TModel>.AlwaysFalseExpression : null;
        }

        var memberBody = prop.Body;
        var body = value switch
        {
            { IsNull: true } => Expression.Equal(memberBody, Expression.Constant(null, typeof(TProperty))),
            { IsNotNull: true } => Expression.NotEqual(memberBody, Expression.Constant(null, typeof(TProperty))),
            _ => null
        };

        if (body == null)
        {
            return null;
        }

        var lam = (Expression<Func<TModel, bool>>)Expression.Lambda(body, prop.Parameters);
        return lam;
    }
}
