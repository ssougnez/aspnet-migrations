namespace AreaProg.AspNetCore.Migrations.Interfaces;

using AreaProg.AspNetCore.Migrations.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for an application migration engine.
/// </summary>
public interface IApplicationMigrationEngine : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the migrations have been applied.
    /// </summary>
    bool HasRun { get; }

    /// <summary>
    /// Applies pending migrations synchronously.
    /// </summary>
    /// <remarks>
    /// This method blocks until all migrations are applied.
    /// For async scenarios, prefer <see cref="RunAsync()"/>.
    /// </remarks>
    void Run();

    /// <summary>
    /// Applies pending migrations synchronously with the specified options.
    /// </summary>
    /// <param name="options">The migration options controlling runtime behavior.</param>
    /// <remarks>
    /// This method blocks until all migrations are applied.
    /// For async scenarios, prefer <see cref="RunAsync(UseMigrationsOptions)"/>.
    /// </remarks>
    void Run(UseMigrationsOptions options);

    /// <summary>
    /// Applies pending migrations asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    Task RunAsync();

    /// <summary>
    /// Applies pending migrations asynchronously with the specified options.
    /// </summary>
    /// <param name="options">The migration options controlling runtime behavior.</param>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    Task RunAsync(UseMigrationsOptions options);
}
