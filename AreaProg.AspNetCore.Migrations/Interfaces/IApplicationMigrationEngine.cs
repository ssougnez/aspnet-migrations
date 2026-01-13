namespace AreaProg.AspNetCore.Migrations.Interfaces;

using System.Threading.Tasks;

/// <summary>
/// Defines the contract for an application migration engine.
/// </summary>
public interface IApplicationMigrationEngine
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
    /// For async scenarios, prefer <see cref="RunAsync"/>.
    /// </remarks>
    void Run();

    /// <summary>
    /// Applies pending migrations asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    Task RunAsync();
}
