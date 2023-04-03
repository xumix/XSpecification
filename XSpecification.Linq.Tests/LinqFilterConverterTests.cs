#nullable disable
using System;
using System.Linq;

using AutoFixture;
using AutoFixture.Kernel;

using LinqKit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using NUnit.Framework;

using XSpecification.Core;

namespace XSpecification.Linq.Tests
{
    [TestFixture]
    public class LinqFilterConverterTests
    {
        private ServiceProvider serviceProvider = null!;

        [OneTimeSetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger<LinqTestSpec>>(Substitute.For<ILogger<LinqTestSpec>>());
            services.AddSingleton<ILogger<IncompatibleLinqTestSpec>>(
                Substitute.For<ILogger<IncompatibleLinqTestSpec>>());

            services.AddLinqSpecification(options =>
            {
                options.AddSpecification<LinqTestSpec>();
                options.AddSpecification<UnhandledLinqTestSpec>();
                options.AddSpecification<IncompatibleLinqTestSpec>();
            });
            // services.AddLinqSpecification(o =>
            // {
            //     o.DisableAutoPropertyHandling = true;
            // });

            serviceProvider = services.BuildServiceProvider();
        }

        [Test]
        public void Ensure_Converters_Dont_Throw()
        {
            var spec = serviceProvider.GetRequiredService<LinqTestSpec>();

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
            var spec = serviceProvider.GetRequiredService<LinqTestSpec>();

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
            var models = context.CreateMany<LinqTestModel>(10);

            var filtered = models.AsQueryable().Where(expression).ToArray();
        }

        [Test]
        public void Ensure_Unhandled_Throws()
        {
            var spec = serviceProvider.GetRequiredService<UnhandledLinqTestSpec>();

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
            var spec = serviceProvider.GetRequiredService<IncompatibleLinqTestSpec>();

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
                    serviceProvider.ValidateSpecifications();
                },
                Throws.InstanceOf<AggregateException>()
                      .And.Message.Contains(nameof(IncompatibleLinqTestFilter.Incompatible)));
        }
    }

    public class LinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
    {
        /// <inheritdoc />
        public LinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options)
            : base(logger, options)
        {
            IgnoreField(f => f.Ignored);
            HandleField(f => f.Explicit, m => m.UnmatchingProperty);
            HandleField(f => f.Conditional, (prop, filter) =>
            {
                if (filter.Conditional)
                {
                    return CreateExpressionFromFilterProperty(prop, f => f.Name, filter.Conditional.ToString());
                }

                if (!filter.Conditional && filter.Id == 312)
                {
                    return PredicateBuilder.New<LinqTestModel>()
                                           .And(f => f.Date.Hour == 1)
                                           .And(f => f.UnmatchingProperty == 123);
                }

                return null;
            });
            HandleField(f => f.NameOrListName, (prop, filter) =>
            {
                return CreateExpressionFromFilterProperty(prop, f => f.Name, filter.NameOrListName)
                    .Or(CreateExpressionFromFilterProperty(prop, f => f.ListName, filter.NameOrListName));
            });
        }
    }

    public class UnhandledLinqTestSpec : SpecificationBase<LinqTestModel, LinqTestFilter>
    {
        /// <inheritdoc />
        public UnhandledLinqTestSpec(ILogger<LinqTestSpec> logger, IOptions<Options> options)
            : base(logger, options)
        {
        }
    }

    public class IncompatibleLinqTestSpec : SpecificationBase<LinqTestModel, IncompatibleLinqTestFilter>
    {
        /// <inheritdoc />
        public IncompatibleLinqTestSpec(ILogger<IncompatibleLinqTestSpec> logger, IOptions<Options> options)
            : base(logger, options)
        {
            HandleField(f => f.Explicit, m => m.UnmatchingProperty);
            HandleField(f => f.Incompatible, m => m.RangeDate);
        }
    }

    public class LinqTestModel
    {
        public int Id { get; set; }

        public int RangeId { get; set; }

        public int ListId { get; set; }

        public string Name { get; set; }

        public string ListName { get; set; }

        public string ComplexName { get; set; }

        public DateTime? NullableDate { get; set; }

        public DateTime Date { get; set; }

        public DateTime ListDate { get; set; }

        public DateTime? RangeDate { get; set; }

        public int UnmatchingProperty { get; set; }
    }

    public class LinqTestFilter
    {
        public int? Id { get; set; }

        public RangeFilter<int> RangeId { get; set; }

        public ListFilter<int> ListId { get; set; }

        public string Name { get; set; }

        public ListFilter<string> ListName { get; set; }

        public StringFilter ComplexName { get; set; }

        public DateTime? NullableDate { get; set; }

        public DateTime? Date { get; set; }

        public ListFilter<DateTime> ListDate { get; set; }

        public RangeFilter<DateTime> RangeDate { get; set; }

        public ListFilter<int> Explicit { get; set; }

        public bool Conditional { get; set; }

        public string Ignored { get; set; }

        public string NameOrListName { get; set; }
    }

    public class IncompatibleLinqTestFilter : LinqTestFilter
    {
        public ListFilter<int> Incompatible { get; set; }
    }
}
