#nullable disable
using System;
using System.Collections;
using System.Linq;

using AutoFixture;
using AutoFixture.Kernel;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using NUnit.Framework;

using XSpecification.Core;
using XSpecification.Linq.Handlers;

namespace XSpecification.Linq.Tests
{
    [TestFixture]
    public class LinqFilterConverterTests
    {
        private ServiceProvider _serviceProvider = null!;

        [OneTimeSetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger<LinqTestSpec>>(Substitute.For<ILogger<LinqTestSpec>>());
            services.AddSingleton<ILogger<IncompatibleLinqTestSpec>>(
                Substitute.For<ILogger<IncompatibleLinqTestSpec>>());

            services.AddLinqSpecification(cfg =>
            {
                cfg.AddSpecification<LinqTestSpec>();
                cfg.AddSpecification<UnhandledLinqTestSpec>();
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
            var filter = context.Create<LinqTestFilter>();

            var expression = spec.CreateFilterExpression(filter);
        }

        [Test]
        public void Ensure_Filtering_Doesnt_Throw()
        {
            var spec = _serviceProvider.GetRequiredService<LinqTestSpec>();

            var filter = new LinqTestFilter
            {
                Date = DateTime.Today,
                Id = 123,
                Name = "qwe",
                ComplexName = new StringFilter("complex") { Contains = true },
                ListDate = new[] { DateTime.Today, DateTime.Today.AddDays(1) },
                ListId = new[] { 1, 2, 3 },
                ListName = new[] { "a", "b", "z" },
                NullableDate = DateTime.Today.AddDays(-1),
                RangeDate = new RangeFilter<DateTime> { Start = DateTime.Today, End = DateTime.Today.AddDays(1) },
                RangeId = new RangeFilter<int> { Start = 0, End = 5 }
            };

            var expression = spec.CreateFilterExpression(filter);

            var fixture = new Fixture();
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                   .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(2));
            var context = new SpecimenContext(fixture);
            var models = context.CreateMany<LinqTestModel>(10).ToArray();

            models.AsQueryable().Where(expression).ToArray();

            filter.RangeId = new RangeFilter<int> { Start = null, End = 5 };
            filter.ListId = new ListFilter<int>();
            filter.ComplexName = new StringFilter("complex");

            expression = spec.CreateFilterExpression(filter);
            models.AsQueryable().Where(expression).ToArray();

            filter.RangeId = new RangeFilter<int>();
            filter.ComplexName = new StringFilter { IsNull = true };

            expression = spec.CreateFilterExpression(filter);
            models.AsQueryable().Where(expression).ToArray();

            filter.RangeId = filter.RangeId = new RangeFilter<int> { End = null, Start = 5 };
            filter.ComplexName = new StringFilter { IsNotNull = true };

            expression = spec.CreateFilterExpression(filter);
            models.AsQueryable().Where(expression).ToArray();
        }

        [Test]
        public void Ensure_Unhandled_Throws()
        {
            var spec = _serviceProvider.GetRequiredService<UnhandledLinqTestSpec>();

            var filter = new LinqTestFilter();

            Assert.That(() =>
                {
                    var expression = spec.CreateFilterExpression(filter);
                },
                Throws.InstanceOf<InvalidOperationException>().And.Message.Contains(nameof(LinqTestFilter.Explicit)));
        }

        [Test]
        public void Ensure_Incompatible_Throws()
        {
            var spec = _serviceProvider.GetRequiredService<IncompatibleLinqTestSpec>();

            var filter = new IncompatibleLinqTestFilter { Incompatible = new ListFilter<int> { 1, 2 } };

            Assert.That(() =>
                {
                    var expression = spec.CreateFilterExpression(filter);
                },
                Throws.InstanceOf<AggregateException>()
                      .And.Message.Contains(nameof(IncompatibleLinqTestFilter.Incompatible)));
        }

        [Test]
        public void Ensure_Validation_Throws()
        {
            Assert.That(() =>
                {
                    _serviceProvider.ValidateSpecifications();
                },
                Throws.InstanceOf<AggregateException>()
                      .And.Message.Contains(nameof(IncompatibleLinqTestFilter.Incompatible)));
        }
    }
}
