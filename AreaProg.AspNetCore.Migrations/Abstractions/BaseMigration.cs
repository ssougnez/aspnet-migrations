namespace AreaProg.AspNetCore.Migrations.Abstractions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// Base class to define an application migration.
/// </summary>
/// <remarks>
/// <para>
/// By default, the current version migration is re-executed on each application startup. This is by design
/// to facilitate development workflows: you can iterate on a migration without having to manually rollback
/// the database version each time.
/// </para>
/// <para>
/// For production environments, you can disable re-execution by setting <c>EnforceLatestMigration = false</c>
/// in <see cref="Models.UseMigrationsOptions"/>. This ensures only new migrations (versions strictly greater
/// than the current registered version) are executed.
/// </para>
/// <para>
/// To handle re-execution, use one of these strategies:
/// <list type="bullet">
///   <item>Use <see cref="FirstTime"/> to guard operations that should only run once (e.g., data inserts)</item>
///   <item>Design your migration methods to be idempotent (safe to re-execute)</item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class BaseMigration
{
    /// <summary>
    /// Application migration version.
    /// </summary>
    public abstract Version Version { get; }

    /// <summary>
    /// Indicates whether this is the first time this specific version is being applied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is <c>true</c> when the migration version has never been registered before,
    /// and <c>false</c> on subsequent re-executions of the same version.
    /// </para>
    /// <para>
    /// Since the current version migration is re-executed by default, use this property to
    /// distinguish between first-time execution and re-execution.
    /// </para>
    /// <para>
    /// Use this property to guard operations that should only execute once:
    /// <code>
    /// public override async Task UpAsync()
    /// {
    ///     if (FirstTime)
    ///     {
    ///         // Insert seed data, send notifications, etc.
    ///     }
    ///
    ///     // Idempotent operations can run every time
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// During debugging, you can still re-run "first time only" code by moving the
    /// execution pointer past the <c>if (FirstTime)</c> check.
    /// </para>
    /// </remarks>
    public bool FirstTime { get; internal set; }

    /// <summary>
    /// Gets a cache dictionary for passing data between <see cref="PrepareMigrationAsync"/> and <see cref="UpAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cache contains data captured before EF Core migrations were applied.
    /// Use it to access preserved data that was transformed by schema changes.
    /// </para>
    /// <para>
    /// Each migration has its own isolated cache instance.
    /// </para>
    /// </remarks>
    public IDictionary<string, object> Cache { get; internal set; } = new Dictionary<string, object>();

    /// <summary>
    /// Called before EF Core database migrations to capture data that will be transformed by schema changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this method to capture data that needs to be preserved and transformed after a schema change.
    /// This is useful when changing column types (e.g., enum to string) where you need to read old values
    /// before the schema changes and write transformed values afterward.
    /// </para>
    /// <para>
    /// This method is only called when there are pending EF Core migrations.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// public override async Task PrepareMigrationAsync(IDictionary&lt;string, object&gt; cache)
    /// {
    ///     // Capture data before schema change
    ///     var oldStatuses = await _db.Database
    ///         .SqlQueryRaw&lt;OldStatus&gt;("SELECT Id, Status FROM Orders")
    ///         .ToListAsync();
    ///     cache["OrderStatuses"] = oldStatuses;
    /// }
    ///
    /// public override async Task UpAsync()
    /// {
    ///     if (Cache.TryGetValue("OrderStatuses", out var data))
    ///     {
    ///         // Transform data after schema change
    ///     }
    /// }
    /// </code>
    /// </remarks>
    /// <param name="cache">
    /// A dictionary to store data that will be available in <see cref="Cache"/> during <see cref="UpAsync"/>.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task PrepareMigrationAsync(IDictionary<string, object> cache) => Task.CompletedTask;

    /// <summary>
    /// Method called to apply the migration.
    /// </summary>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    public abstract Task UpAsync();
}
