namespace AreaProg.Migrations.Demo.Migrations;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.Extensions;

/// <summary>
/// Application migration engine that stores version history in the database.
/// </summary>
/// <remarks>
/// <para>
/// This demonstrates the simplified v2 approach using <see cref="EfCoreMigrationEngine"/>.
/// The base class handles all version tracking automatically.
/// </para>
/// <para>
/// For SQL Server deployments with multiple instances, inherit from
/// <see cref="SqlServerMigrationEngine"/> instead to enable distributed locking.
/// </para>
/// </remarks>
public class AppMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider,
    ILogger<AppMigrationEngine> logger,
    IConfiguration configuration
) : EfCoreMigrationEngine(serviceProvider, options.DbContext)
{
    /// <summary>
    /// Controls whether migrations should run.
    /// Can be disabled via configuration for specific environments.
    /// </summary>
    public override Task<bool> ShouldRunAsync()
        => Task.FromResult(configuration.GetValue("Migrations:Enabled", defaultValue: true));

    /// <summary>
    /// Called before any migrations run.
    /// Use this for initialization or validation.
    /// </summary>
    public override Task RunBeforeAsync()
    {
        logger.LogInformation("Starting application migrations...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after all migrations complete.
    /// Use this for cleanup or finalization.
    /// </summary>
    public override Task RunAfterAsync()
    {
        logger.LogInformation("Application migrations completed successfully.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called before EF Core database migrations.
    /// Use this for global setup before schema changes (e.g., logging, validation).
    /// For capturing data before schema changes, use PrepareMigrationAsync in individual migrations.
    /// </summary>
    public override Task RunBeforeDatabaseMigrationAsync()
    {
        logger.LogInformation("Preparing for database schema migrations...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after EF Core database migrations complete.
    /// Use this for post-schema-change tasks.
    /// </summary>
    public override Task RunAfterDatabaseMigrationAsync()
    {
        logger.LogInformation("Database schema migrations completed.");
        return Task.CompletedTask;
    }
}
