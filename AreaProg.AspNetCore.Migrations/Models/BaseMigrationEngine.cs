namespace AreaProg.AspNetCore.Migrations.Models;

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
/// <see cref="RunAfterDatabaseMigration"/>) that can be overridden to execute custom logic during the migration process.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class BaseMigrationEngine
{
    /// <summary>
    /// Gets a value indicating whether migrations should be executed.
    /// </summary>
    /// <remarks>
    /// Override this property to conditionally skip migrations based on environment, configuration, or other criteria.
    /// </remarks>
    /// <value><c>true</c> to execute migrations; <c>false</c> to skip. Default is <c>true</c>.</value>
    public virtual bool ShouldRun => true;

    /// <summary>
    /// Returns all application versions that have been previously applied.
    /// </summary>
    /// <returns>An array of versions that have been applied, used to determine which migrations to run.</returns>
    public abstract Task<Version[]> GetAppliedVersionAsync();

    /// <summary>
    /// Returns the current version of the application.
    /// </summary>
    /// <returns>The current application version.</returns>
    public abstract Task<Version> GetCurrentVersionAsync();

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
    /// Called immediately after Entity Framework Core database migrations have been applied.
    /// </summary>
    /// <remarks>
    /// Override this method to perform tasks that depend on the database schema being up to date,
    /// but before application migrations run.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task RunAfterDatabaseMigration() => Task.CompletedTask;
}
