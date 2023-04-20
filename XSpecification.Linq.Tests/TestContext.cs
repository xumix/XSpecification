#nullable disable
using Microsoft.EntityFrameworkCore;

namespace XSpecification.Linq.Tests;

public class TestContext : DbContext
{
    /// <inheritdoc />
    public TestContext(DbContextOptions<TestContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LinqTestModel>();
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<LinqTestModel> TestModels { get; set; }
}
