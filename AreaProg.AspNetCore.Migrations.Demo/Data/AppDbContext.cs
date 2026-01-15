using AreaProg.AspNetCore.Migrations.Demo.Data.Entities;
using AreaProg.AspNetCore.Migrations.Models;
using Microsoft.EntityFrameworkCore;

namespace AreaProg.AspNetCore.Migrations.Demo.Data;

/// <summary>
/// Application database context for the demo application.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AppliedMigration> AppliedMigrations => Set<AppliedMigration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<AppliedMigration>(entity =>
        {
            entity.ToTable("AppliedMigrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Version).IsUnique();
        });
    }
}
