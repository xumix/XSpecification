#nullable disable

using System;

using XSpecification.Core;
using XSpecification.Elasticsearch.Handlers;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elastic.Tests;

public class TestFilterHandler : IFilterHandler
{
    /// <inheritdoc />
    public virtual void Handle(QueryContext context, Action<QueryContext> next)
    {
        next(context);
    }

    public virtual bool CanHandle(QueryContext context)
    {
        return context.FilterPropertyValue is not IFilter;
    }
}
