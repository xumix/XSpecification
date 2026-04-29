#nullable disable
using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using XSpecification.Core;
using XSpecification.Core.Pipeline;
using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Handlers;
using XSpecification.Elasticsearch.Pipeline;

namespace XSpecification.Elastic.Tests;

[TestFixture]
public class PipelineHandlerTests
{
    private static ServiceProvider BuildProvider(
        Action<IRegistrationConfigurator<ElasticFilterHandlerCollection>> configureAction)
    {
        var services = new ServiceCollection();
        services.AddLogging(c =>
        {
            c.AddConsole().AddDebug();
            c.SetMinimumLevel(LogLevel.Trace);
        });

        services.AddElasticSpecification(cfg =>
        {
            cfg.AddSpecification<ElasticPipelineOrderSpec>();
            configureAction(cfg);
        });

        return services.BuildServiceProvider();
    }

    [Test]
    public void Pipeline_Invokes_Handlers_In_Order()
    {
        using var provider = BuildProvider(cfg =>
        {
            cfg.FilterHandlers.AddFirst(typeof(TraceHandlerB));
            cfg.FilterHandlers.AddFirst(typeof(TraceHandlerA));
        });

        var spec = provider.GetRequiredService<ElasticPipelineOrderSpec>();
        var traceFilter = new TraceFilter();
        var filter = new ElasticPipelineOrderFilter { Trace = traceFilter };

        _ = spec.CreateFilterQuery(filter);

        traceFilter.Trace.Should().Equal("A", "B");
    }

    [Test]
    public void Pipeline_ShortCircuits_When_Handler_Does_Not_Call_Next()
    {
        using var provider = BuildProvider(cfg =>
        {
            cfg.FilterHandlers.AddFirst(typeof(TraceHandlerB));
            cfg.FilterHandlers.AddFirst(typeof(TraceHandlerShortCircuit));
        });

        var spec = provider.GetRequiredService<ElasticPipelineOrderSpec>();
        var traceFilter = new TraceFilter();
        var filter = new ElasticPipelineOrderFilter { Trace = traceFilter };

        _ = spec.CreateFilterQuery(filter);

        traceFilter.Trace.Should().Equal("ShortCircuit");
    }
}

public class ElasticPipelineOrderSpec : SpecificationBase<ElasticPipelineOrderModel, ElasticPipelineOrderFilter>
{
    public ElasticPipelineOrderSpec(
        ILogger<ElasticPipelineOrderSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
    }
}

public class ElasticPipelineOrderFilter
{
    public TraceFilter Trace { get; set; } = new TraceFilter();
}

public class ElasticPipelineOrderModel
{
    public TraceFilter Trace { get; set; } = new TraceFilter();
}

public class TraceFilter : IFilter
{
    public List<string> Trace { get; } = new List<string>();
}

public class TraceHandlerA : IFilterHandler
{
    public void Handle(QueryContext context, Action<QueryContext> next)
    {
        var trace = (TraceFilter)context.FilterPropertyValue;
        trace.Trace.Add("A");
        next(context);
    }

    public bool CanHandle(QueryContext context)
    {
        return context.FilterPropertyValue is TraceFilter;
    }
}

public class TraceHandlerB : IFilterHandler
{
    public void Handle(QueryContext context, Action<QueryContext> next)
    {
        var trace = (TraceFilter)context.FilterPropertyValue;
        trace.Trace.Add("B");
        next(context);
    }

    public bool CanHandle(QueryContext context)
    {
        return context.FilterPropertyValue is TraceFilter;
    }
}

public class TraceHandlerShortCircuit : IFilterHandler
{
    public void Handle(QueryContext context, Action<QueryContext> next)
    {
        var trace = (TraceFilter)context.FilterPropertyValue;
        trace.Trace.Add("ShortCircuit");
    }

    public bool CanHandle(QueryContext context)
    {
        return context.FilterPropertyValue is TraceFilter;
    }
}
