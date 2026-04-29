#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    [Test]
    public void Pipeline_Is_Thread_Safe_During_Concurrent_First_Use()
    {
        CountingHandler.Reset();

        using var provider = BuildProvider(cfg =>
        {
            cfg.FilterHandlers.AddFirst(typeof(CountingHandler));
        });

        var pipeline = provider.GetRequiredService<IFilterHandlerPipeline<PipelineOrderModel>>();

        const int parallelism = 32;
        const int iterationsPerThread = 50;

        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, parallelism, _ =>
        {
            try
            {
                for (var i = 0; i < iterationsPerThread; i++)
                {
                    var traceFilter = new TraceFilter();
                    var ctx = new LinqFilterContext<PipelineOrderModel>
                    {
                        FilterProperty = typeof(PipelineOrderFilter).GetProperty(nameof(PipelineOrderFilter.Trace)),
                        FilterPropertyValue = traceFilter,
                        ModelProperty = typeof(PipelineOrderModel).GetProperty(nameof(PipelineOrderModel.Trace)),
                    };
                    pipeline.Execute(ctx);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        exceptions.Should().BeEmpty();
        CountingHandler.HandlerInstanceCount.Should().Be(1, "handler must be a singleton resolved exactly once");
    }
}

public class PipelineOrderSpec : SpecificationBase<PipelineOrderModel, PipelineOrderFilter>
{
    public PipelineOrderSpec(
        ILogger<PipelineOrderSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<PipelineOrderModel> handlerPipeline)
        : base(logger, configuration, handlerPipeline)
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

public class CountingHandler : IFilterHandler
{
    private static int _instanceCount;

    public CountingHandler()
    {
        System.Threading.Interlocked.Increment(ref _instanceCount);
    }

    public static int HandlerInstanceCount => _instanceCount;

    public static void Reset() => System.Threading.Interlocked.Exchange(ref _instanceCount, 0);

    public void Handle<TModel>(LinqFilterContext<TModel> context, Action<LinqFilterContext<TModel>> next)
    {
        next(context);
    }

    public bool CanHandle<TModel>(LinqFilterContext<TModel> context) => true;
}
