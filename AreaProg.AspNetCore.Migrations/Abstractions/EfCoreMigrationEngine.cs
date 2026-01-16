namespace AreaProg.AspNetCore.Migrations.Abstractions;

using AreaProg.AspNetCore.Migrations.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Abstract migration engine that stores version history using Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>
/// This engine provides ready-to-use implementations of <see cref="BaseMigrationEngine.GetAppliedVersionsAsync"/>
/// and <see cref="BaseMigrationEngine.RegisterVersionAsync"/> using the <see cref="AppliedMigration"/> entity.
/// </para>
/// <para>
/// To use this engine, add the <see cref="AppliedMigration"/> entity to your DbContext:
/// </para>
/// <code>
/// public class AppDbContext : DbContext
/// {
///     public DbSet&lt;AppliedMigration&gt; AppliedMigrations { get; set; }
/// }
/// </code>
/// <para>
/// Then create your migration engine:
/// </para>
/// <code>
/// public class AppMigrationEngine : EfCoreMigrationEngine
/// {
///     public AppMigrationEngine(
///         ApplicationMigrationsOptions&lt;AppMigrationEngine&gt; options,
///         IServiceProvider serviceProvider)
///         : base(serviceProvider, options.DbContext) { }
/// }
/// </code>
/// </remarks>
/// <param name="serviceProvider">
/// The service provider to resolve the DbContext from.
/// This should be a scoped provider (as provided by ApplicationMigrationEngine).
/// </param>
/// <param name="dbContextType">
/// The type of DbContext to use, typically from <c>ApplicationMigrationsOptions.DbContext</c>.
/// </param>
public abstract class EfCoreMigrationEngine(IServiceProvider serviceProvider, Type? dbContextType) : BaseMigrationEngine
{
    /// <summary>
    /// Gets the DbContext used for version tracking.
    /// </summary>
    /// <remarks>
    /// May be null if no DbContext type was configured in the migration options.
    /// </remarks>
    protected DbContext? DbContext { get; } = dbContextType is not null ? serviceProvider.GetService(dbContextType) as DbContext : null;

    /// <summary>
    /// Retrieves all previously applied migration versions from the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method handles scenarios where the database or table doesn't exist yet
    /// by first checking connectivity and catching any exceptions during query execution.
    /// </para>
    /// </remarks>
    /// <returns>An array of applied versions, or an empty array if the table doesn't exist yet.</returns>
    public override async Task<Version[]> GetAppliedVersionsAsync()
    {
        if (DbContext is null || !await DbContext.Database.CanConnectAsync())
        {
            return [];
        }

        try
        {
            var versions = await DbContext
                .Set<AppliedMigration>()
                .AsNoTracking()
                .Select(m => m.Version)
                .ToListAsync();

            return [.. versions.Select(Version.Parse)];
        }
        catch
        {
            // Table doesn't exist yet - EF Core migrations will create it
            return [];
        }
    }

    /// <summary>
    /// Registers a version as applied in the database.
    /// </summary>
    /// <remarks>
    /// This method checks for duplicates before inserting to ensure idempotency.
    /// </remarks>
    /// <param name="version">The version to register.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task RegisterVersionAsync(Version version)
    {
        if (DbContext is null)
        {
            return;
        }

        var versionString = version.ToString();

        var exists = await DbContext
            .Set<AppliedMigration>()
            .AnyAsync(m => m.Version == versionString);

        if (!exists)
        {
            DbContext.Set<AppliedMigration>().Add(new AppliedMigration
            {
                Version = versionString,
                AppliedAt = DateTime.UtcNow
            });

            await DbContext.SaveChangesAsync();
        }
    }
}
