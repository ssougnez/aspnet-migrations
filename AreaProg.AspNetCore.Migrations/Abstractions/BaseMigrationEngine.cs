namespace AreaProg.AspNetCore.Migrations.Abstractions;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// Abstract base class for implementing a migration engine that tracks and manages application versions.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to implement custom version storage (e.g., database table, file system, external service).
/// </para>
/// <para>
/// The engine provides lifecycle hooks (<see cref="RunBeforeAsync"/>, <see cref="RunAfterAsync"/>,
/// <see cref="RunBeforeDatabaseMigrationAsync"/>, <see cref="RunAfterDatabaseMigrationAsync"/>)
/// that can be overridden to execute custom logic during the migration process.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class BaseMigrationEngine
{
    /// <summary>
    /// Determines whether migrations should be executed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this method to conditionally skip migrations based on environment, configuration,
    /// or to implement distributed locking (e.g., using <c>sp_getapplock</c> for SQL Server).
    /// </para>
    /// <para>
    /// This method is asynchronous to support scenarios like acquiring a database lock
    /// before running migrations in multi-instance deployments.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A task that resolves to <c>true</c> to execute migrations; <c>false</c> to skip.
    /// Default implementation returns <c>true</c>.
    /// </returns>
    public virtual Task<bool> ShouldRunAsync() => Task.FromResult(true);

    /// <summary>
    /// Returns all application versions that have been previously applied.
    /// </summary>
    /// <returns>An array of versions that have been applied, used to determine which migrations to run.</returns>
    public abstract Task<Version[]> GetAppliedVersionsAsync();

    /// <summary>
    /// Registers a version as applied in the external storage system.
    /// </summary>
    /// <param name="version">The version to register.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task RegisterVersionAsync(Version version);

    /// <summary>
    /// Called after all migrations have been applied.
    /// </summary>
    /// <remarks>
    /// Override this method to perform cleanup or post-migration tasks.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task RunAfterAsync() => Task.CompletedTask;

    /// <summary>
    /// Called before any migrations are applied.
    /// </summary>
    /// <remarks>
    /// Override this method to perform setup or pre-migration validation.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task RunBeforeAsync() => Task.CompletedTask;

    /// <summary>
    /// Called immediately before Entity Framework Core database migrations are applied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this method to perform global setup before EF Core migrations run,
    /// such as logging or validation.
    /// </para>
    /// <para>
    /// This hook is only called when there are pending EF Core migrations.
    /// </para>
    /// <para>
    /// For capturing data before schema changes, use <see cref="BaseMigration.PrepareMigrationAsync"/>
    /// in individual migrations instead.
    /// </para>
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task RunBeforeDatabaseMigrationAsync() => Task.CompletedTask;

    /// <summary>
    /// Called immediately after Entity Framework Core database migrations have been applied.
    /// </summary>
    /// <remarks>
    /// Override this method to perform tasks that depend on the database schema being up to date,
    /// but before application migrations run.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task RunAfterDatabaseMigrationAsync() => Task.CompletedTask;
}
