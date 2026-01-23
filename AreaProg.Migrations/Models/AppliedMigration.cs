namespace AreaProg.Migrations.Models;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Entity for tracking applied application migrations in the database.
/// </summary>
/// <remarks>
/// <para>
/// Add this entity to your <see cref="Microsoft.EntityFrameworkCore.DbContext"/> to use
/// <see cref="Abstractions.EfCoreMigrationEngine"/> or <see cref="Abstractions.SqlServerMigrationEngine"/>:
/// </para>
/// <code>
/// public class AppDbContext : DbContext
/// {
///     public DbSet&lt;AppliedMigration&gt; AppliedMigrations { get; set; }
/// }
/// </code>
/// <para>
/// After adding the entity, create an EF Core migration to generate the table:
/// </para>
/// <code>
/// dotnet ef migrations add AddAppliedMigrations
/// </code>
/// </remarks>
public class AppliedMigration
{
    /// <summary>
    /// Gets or sets the unique identifier for this migration record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the version string of the applied migration.
    /// </summary>
    /// <remarks>
    /// Stored as a string representation of <see cref="System.Version"/> (e.g., "1.0.0", "2.1.3").
    /// </remarks>
    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this migration was applied.
    /// </summary>
    public DateTime AppliedAt { get; set; }
}
