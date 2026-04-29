#nullable disable
using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using XSpecification.Core;
using XSpecification.Core.Pipeline;
using XSpecification.Linq.Handlers;
using XSpecification.Linq.Pipeline;
using XSpecification.Linq.Tests.Specs;

namespace XSpecification.Linq.Tests;

[TestFixture]
public class PipelineHandlerTests
{
    private static ServiceProvider BuildProvider(
        Action<IRegistrationConfigurator<LinqFilterHandlerCollection>> configureAction)
    {
        var services = new ServiceCollection();
        services.AddLogging(c =>
        {
            c.AddConsole().AddDebug();
            c.SetMinimumLevel(LogLevel.Trace);
        });

        services.AddLinqSpecification(cfg =>
        {
            cfg.AddSpecification<PipelineOrderSpec>();
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

        var spec = provider.GetRequiredService<PipelineOrderSpec>();
        var traceFilter = new TraceFilter();
        var filter = new PipelineOrderFilter { Trace = traceFilter };

        _ = spec.CreateFilterExpression(filter);

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

        var spec = provider.GetRequiredService<PipelineOrderSpec>();
        var traceFilter = new TraceFilter();
        var filter = new PipelineOrderFilter { Trace = traceFilter };

        _ = spec.CreateFilterExpression(filter);

        traceFilter.Trace.Should().Equal("ShortCircuit");
    }
}

public class PipelineOrderSpec : SpecificationBase<PipelineOrderModel, PipelineOrderFilter>
{
    public PipelineOrderSpec(
        ILogger<PipelineOrderSpec> logger,
        Microsoft.Extensions.Options.IOptions<Options> options,
        IFilterHandlerPipeline<PipelineOrderModel> handlerPipeline)
        : base(logger, options, handlerPipeline)
    {
    }
}

public class PipelineOrderFilter
{
    public TraceFilter Trace { get; set; } = new TraceFilter();
}

public class PipelineOrderModel
{
    public TraceFilter Trace { get; set; } = new TraceFilter();
}

public class TraceFilter : IFilter
{
    public List<string> Trace { get; } = new List<string>();
}

public class TraceHandlerA : IFilterHandler
{
    public void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        var trace = (TraceFilter)context.FilterPropertyValue;
        trace.Trace.Add("A");
        next(context);
    }

    public bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        return context.FilterPropertyValue is TraceFilter;
    }
}

public class TraceHandlerB : IFilterHandler
{
    public void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        var trace = (TraceFilter)context.FilterPropertyValue;
        trace.Trace.Add("B");
        next(context);
    }

    public bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        return context.FilterPropertyValue is TraceFilter;
    }
}

public class TraceHandlerShortCircuit : IFilterHandler
{
    public void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        var trace = (TraceFilter)context.FilterPropertyValue;
        trace.Trace.Add("ShortCircuit");
    }

    public bool CanHandle<TModel>(LinqFilterContext<TModel> context)
    {
        return context.FilterPropertyValue is TraceFilter;
    }
}
