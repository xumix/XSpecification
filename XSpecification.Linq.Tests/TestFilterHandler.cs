#nullable disable
using System;

using XSpecification.Core;
using XSpecification.Linq.Handlers;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests;

public class TestFilterHandler : IFilterHandler
{
    /// <inheritdoc />
    public virtual void CreateExpression<TModel>(Context<TModel> context, Action<Context<TModel>> next)
    {
        next(context);
    }

    public virtual bool CanHandle<TModel>(Context<TModel> context)
    {
        return context.FilterPropertyValue is not IFilter;
    }
}
