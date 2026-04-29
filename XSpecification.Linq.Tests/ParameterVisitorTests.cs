#nullable disable
using System;
using System.Linq.Expressions;

using FluentAssertions;

using NUnit.Framework;

using XSpecification.Core;

namespace XSpecification.Linq.Tests;

[TestFixture]
public class ParameterVisitorTests
{
    [Test]
    public void AndAlso_Merges_Parameters_Correctly()
    {
        Expression<Func<LinqTestModel, bool>> left = x => x.Id > 1;
        Expression<Func<LinqTestModel, bool>> right = y => y.Id < 10;

        var combined = ParameterVisitor.AndAlso(left, right);
        var predicate = combined.Compile();

        predicate(new LinqTestModel { Id = 5 }).Should().BeTrue();
        predicate(new LinqTestModel { Id = 0 }).Should().BeFalse();
        predicate(new LinqTestModel { Id = 15 }).Should().BeFalse();
    }
}
