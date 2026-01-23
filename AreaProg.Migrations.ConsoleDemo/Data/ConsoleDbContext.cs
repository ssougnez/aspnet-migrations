using AreaProg.Migrations.Models;
using Microsoft.EntityFrameworkCore;

namespace AreaProg.Migrations.ConsoleDemo.Data;

public class ConsoleDbContext : DbContext
{
    public ConsoleDbContext(DbContextOptions<ConsoleDbContext> options) : base(options)
    {
    }

    public DbSet<AppliedMigration> AppliedMigrations { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
}

public class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
