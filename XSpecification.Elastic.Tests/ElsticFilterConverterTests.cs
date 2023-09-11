#nullable disable

using System;
using System.Collections;
using System.Linq;

using AutoFixture;
using AutoFixture.Kernel;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using XSpecification.Core;
using XSpecification.Elastic.Tests.Specs;
using XSpecification.Elasticsearch;
using XSpecification.Elasticsearch.Handlers;

namespace XSpecification.Elastic.Tests
{
    [TestFixture]
    public class ElsticFilterConverterTests
    {
        private ServiceProvider _serviceProvider = null!;

        [OneTimeSetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddLogging(c =>
            {
                c.AddConsole().AddDebug();
                c.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddElasticSpecification(cfg =>
            {
                cfg.AddSpecification<LinqTestSpec>();
                cfg.AddSpecification<UnhandledTestSpec>();
                cfg.AddSpecification<IncompatibleLinqTestSpec>();

                cfg.FilterHandlers.AddBefore<ConstantFilterHandler>(typeof(TestFilterHandler));
                cfg.FilterHandlers.AddAfter<NullableFilterHandler>(typeof(TestFilterHandler2));

                // for coverage
                var enumerator = ((IEnumerable)cfg.FilterHandlers).GetEnumerator();
                enumerator.MoveNext();
                enumerator.Current.Should().NotBeNull();
                enumerator.Reset();
            });

            // services.AddLinqSpecification(o =>
            // {
            //     o.DisableAutoPropertyHandling = true;
            // });

            _serviceProvider = services.BuildServiceProvider();
        }

        [Test]
        public void Ensure_Converters_Dont_Throw()
        {
            var spec = _serviceProvider.GetRequiredService<LinqTestSpec>();

            var fixture = new Fixture();
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                   .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(2));
            fixture.Customize<INullableFilter>(c => c.With(f => f.IsNull, false).With(f => f.IsNotNull, false));

            var context = new SpecimenContext(fixture);
            for (var i = 0; i < 10; i++)
            {
                var filter = context.Create<ElsaticTestFilter>();
                var expression = spec.CreateFilterQuery(filter);
            }
        }

        // [Test]
        // public void Ensure_Filtering_Doesnt_Throw()
        // {
        //     var spec = _serviceProvider.GetRequiredService<LinqTestSpec>();
        //
        //     using var dbContext = _serviceProvider.GetRequiredService<TestContext>();
        //     dbContext.Database.EnsureCreated();
        //
        //     var filter = new ElsaticTestFilter
        //     {
        //         Date = DateTime.Today,
        //         Id = 123,
        //         Name = "qwe",
        //         ComplexName = new StringFilter("complex") { Contains = true },
        //         ListDate = new[] { DateTime.Today, DateTime.Today.AddDays(1) },
        //         ListId = new[] { 1, 2, 3 },
        //         ListName = new[] { "a", "b", "z" },
        //         NullableDate = DateTime.Today.AddDays(-1),
        //         RangeDate = new RangeFilter<DateTime> { Start = DateTime.Today, End = DateTime.Today.AddDays(1) },
        //         RangeId = new RangeFilter<int> { Start = 0, End = 5 }
        //     };
        //
        //     var expression = spec.CreateFilterQuery(filter);
        //
        //     var fixture = new Fixture();
        //     fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
        //            .ForEach(b => fixture.Behaviors.Remove(b));
        //     fixture.Behaviors.Add(new OmitOnRecursionBehavior(2));
        //     var context = new SpecimenContext(fixture);
        //     var models = context.CreateMany<ElasticTestModel>(10).ToArray();
        //
        //     models.AsQueryable().Where(expression).ToArray();
        //     dbContext.TestModels.AddRange(models);
        //     dbContext.SaveChanges();
        //
        //     dbContext.TestModels.Where(expression).ToArray();
        //
        //     filter.RangeId = new RangeFilter<int> { Start = null, End = 5 };
        //     filter.ListId = new ListFilter<int>();
        //     filter.ComplexName = new StringFilter("complex");
        //
        //     expression = spec.CreateFilterQuery(filter);
        //     models.AsQueryable().Where(expression).ToArray();
        //     dbContext.TestModels.Where(expression).ToArray();
        //
        //     filter.RangeId = new RangeFilter<int>();
        //     filter.ComplexName = new StringFilter { IsNull = true };
        //
        //     expression = spec.CreateFilterQuery(filter);
        //     models.AsQueryable().Where(expression).ToArray();
        //     dbContext.TestModels.Where(expression).ToArray();
        //
        //     filter.RangeId = filter.RangeId = new RangeFilter<int> { End = null, Start = 5 };
        //     filter.ComplexName = new StringFilter { IsNotNull = true };
        //
        //     expression = spec.CreateFilterQuery(filter);
        //     models.AsQueryable().Where(expression).ToArray();
        //     dbContext.TestModels.Where(expression).ToArray();
        // }

        [Test]
        public void Ensure_Unhandled_Throws()
        {
            var spec = _serviceProvider.GetRequiredService<UnhandledTestSpec>();

            var filter = new ElsaticTestFilter();

            Assert.That(() =>
                {
                    var expression = spec.CreateFilterQuery(filter);
                },
                Throws.InstanceOf<InvalidOperationException>().And.Message.Contains(nameof(ElsaticTestFilter.Explicit)));
        }

        [Test]
        public void Ensure_Incompatible_Throws()
        {
            var spec = _serviceProvider.GetRequiredService<IncompatibleLinqTestSpec>();

            var filter = new IncompatibleElsaticTestFilter { Incompatible = new ListFilter<int> { 1, 2 } };

            Assert.That(() =>
                {
                    var expression = spec.CreateFilterQuery(filter);
                },
                Throws.InstanceOf<AggregateException>()
                      .And.Message.Contains(nameof(IncompatibleElsaticTestFilter.Incompatible)));
        }

        [Test]
        public void Ensure_Validation_Throws()
        {
            Assert.That(() =>
                {
                    _serviceProvider.ValidateSpecifications();
                },
                Throws.InstanceOf<AggregateException>()
                      .And.Message.Contains("are not mapped"));
        }
    }
}
