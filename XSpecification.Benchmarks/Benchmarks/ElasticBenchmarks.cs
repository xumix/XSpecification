using BenchmarkDotNet.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using XSpecification.Benchmarks.Models;
using XSpecification.Benchmarks.Specs;
using XSpecification.Core;
using XSpecification.Elasticsearch;

namespace XSpecification.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class ElasticBenchmarks
{
    private ServiceProvider _provider = null!;
    private BenchElasticSmallSpec _smallSpec = null!;
    private BenchElasticLargeSpec _largeSpec = null!;
    private BenchSmallFilter _smallFilter = null!;
    private BenchLargeFilter _largeFilter = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddElasticSpecification(cfg =>
        {
            cfg.AddSpecification<BenchElasticSmallSpec>();
            cfg.AddSpecification<BenchElasticLargeSpec>();
        });

        _provider = services.BuildServiceProvider();
        _smallSpec = _provider.GetRequiredService<BenchElasticSmallSpec>();
        _largeSpec = _provider.GetRequiredService<BenchElasticLargeSpec>();

        _smallFilter = new BenchSmallFilter
        {
            Id = 42,
            Name = new StringFilter("widget") { Contains = true },
        };

        _largeFilter = new BenchLargeFilter
        {
            Id = 42,
            Name = new StringFilter("widget") { Contains = true },
            Date = new RangeFilter<DateTime>
            {
                Start = new DateTime(2024, 1, 1),
                End = new DateTime(2024, 12, 31),
            },
            Quantity = new RangeFilter<int> { Start = 1, End = 1000 },
            Price = new RangeFilter<decimal> { Start = 0m, End = 9999m },
            IdIn = new ListFilter<int>(1, 2, 3, 4, 5),
            NameIn = new ListFilter<string>("a", "b", "c") { IsInverted = true },
            IsActive = true,
        };

        _ = _smallSpec.CreateFilterQuery(_smallFilter);
        _ = _largeSpec.CreateFilterQuery(_largeFilter);
    }

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    [Benchmark]
    public object SmallFilter() => _smallSpec.CreateFilterQuery(_smallFilter);

    [Benchmark]
    public object LargeFilter() => _largeSpec.CreateFilterQuery(_largeFilter);
}
