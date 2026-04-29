#nullable disable
using System;

using XSpecification.Core;
using XSpecification.Linq.Handlers;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests;

public class TestFilterHandler2 : IFilterHandler
{
    /// <inheritdoc />
    public virtual void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        next(context);
    }

    public virtual bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        return context.FilterPropertyValue is IFilter;
    }
}
