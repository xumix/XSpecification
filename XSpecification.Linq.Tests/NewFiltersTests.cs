#nullable disable
using FluentAssertions;

using LinqKit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

using XSpecification.Core;
using XSpecification.Linq.Pipeline;

namespace XSpecification.Linq.Tests;

[TestFixture]
public class NewFiltersTests
{
    [Test]
    public void BoolFilter_HasValue_Distinguishes_Unset_From_False()
    {
        var unset = new BoolFilter();
        unset.HasValue().Should().BeFalse();

        var setFalse = new BoolFilter { Value = false };
        setFalse.HasValue().Should().BeTrue();

        var setNullCheck = new BoolFilter { IsNull = true };
        setNullCheck.HasValue().Should().BeTrue();
    }

    [Test]
    public void BoolFilter_IsNull_And_IsNotNull_Are_Mutually_Exclusive()
    {
        var f = new BoolFilter { IsNull = true };
        f.IsNull.Should().BeTrue();
        f.IsNotNull.Should().BeFalse();

        f.IsNotNull = true;
        f.IsNotNull.Should().BeTrue();
        f.IsNull.Should().BeFalse();
    }

    [Test]
    public void OrGroup_Builds_Disjunction_Across_Multiple_Model_Properties()
    {
        var provider = BuildProvider();
        var spec = provider.GetRequiredService<OrGroupSpec>();

        var expression = spec.CreateFilterExpression(new OrGroupFilter { Search = "abc" });

        var compiled = expression.Compile();
        compiled(new OrGroupModel { Name = "abc" }).Should().BeTrue();
        compiled(new OrGroupModel { ListName = "abc" }).Should().BeTrue();
        compiled(new OrGroupModel { Name = "x", ListName = "y" }).Should().BeFalse();
    }

    [Test]
    public void Spec_And_Composition_Combines_Predicates()
    {
        var provider = BuildProvider();
        var orSpec = provider.GetRequiredService<OrGroupSpec>();
        var filter = new OrGroupFilter { Search = "abc" };

        var expression = orSpec.And(orSpec, filter);
        var compiled = expression.Compile();

        compiled(new OrGroupModel { Name = "abc" }).Should().BeTrue();
        compiled(new OrGroupModel { Name = "no" }).Should().BeFalse();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddDebug());
        services.AddLinqSpecification(cfg =>
        {
            cfg.AddSpecification<OrGroupSpec>();
        });
        return services.BuildServiceProvider();
    }
}

public class OrGroupModel
{
    public string Name { get; set; }

    public string ListName { get; set; }
}

public class OrGroupFilter
{
    public string Search { get; set; }
}

public class OrGroupSpec : SpecificationBase<OrGroupModel, OrGroupFilter>
{
    public OrGroupSpec(
        ILogger<OrGroupSpec> logger,
        SpecificationConfiguration configuration,
        IFilterHandlerPipeline<OrGroupModel> handlerPipeline)
        : base(logger, configuration, handlerPipeline)
    {
        OrGroup(f => f.Search, m => m.Name, m => m.ListName);
    }
}
