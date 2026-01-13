namespace AreaProg.AspNetCore.Migrations.Models;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// Base class to define an application migration.
/// </summary>
/// <remarks>
/// <para>
/// The current version migration is re-executed on each application startup. This is by design
/// to facilitate development workflows: you can iterate on a migration without having to
/// manually rollback the database version each time.
/// </para>
/// <para>
/// To handle this behavior, you have two options:
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
    public bool FirstTime { get; set; }

    /// <summary>
    /// Method called to apply the migration.
    /// </summary>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    public abstract Task UpAsync();
}
