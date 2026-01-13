namespace AreaProg.AspNetCore.Migrations.Tests.Fixtures;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Test DbContext for unit testing with InMemory provider.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;
}

/// <summary>
/// Simple test entity.
/// </summary>
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
