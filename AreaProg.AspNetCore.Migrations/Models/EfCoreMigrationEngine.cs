namespace AreaProg.AspNetCore.Migrations.Models;

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
public abstract class EfCoreMigrationEngine : BaseMigrationEngine
{
    /// <summary>
    /// Gets the DbContext used for version tracking.
    /// </summary>
    /// <remarks>
    /// May be null if no DbContext type was configured in the migration options.
    /// </remarks>
    protected DbContext? DbContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreMigrationEngine"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider to resolve the DbContext from.
    /// This should be a scoped provider (as provided by ApplicationMigrationEngine).
    /// </param>
    /// <param name="dbContextType">
    /// The type of DbContext to use, typically from <c>ApplicationMigrationsOptions.DbContext</c>.
    /// </param>
    protected EfCoreMigrationEngine(IServiceProvider serviceProvider, Type? dbContextType)
    {
        DbContext = dbContextType != null
            ? serviceProvider.GetService(dbContextType) as DbContext
            : null;
    }

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
        if (DbContext == null || !await DbContext.Database.CanConnectAsync())
        {
            return Array.Empty<Version>();
        }

        try
        {
            var versions = await DbContext.Set<AppliedMigration>()
                .Select(m => m.Version)
                .ToListAsync();

            return versions
                .Select(v => Version.Parse(v))
                .ToArray();
        }
        catch
        {
            // Table doesn't exist yet - EF Core migrations will create it
            return Array.Empty<Version>();
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
        if (DbContext == null)
        {
            return;
        }

        var versionString = version.ToString();

        var exists = await DbContext.Set<AppliedMigration>()
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
