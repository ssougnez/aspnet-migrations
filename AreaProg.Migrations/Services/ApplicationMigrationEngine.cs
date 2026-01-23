namespace AreaProg.Migrations.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.Extensions;
using AreaProg.Migrations.Interfaces;
using AreaProg.Migrations.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Default implementation of <see cref="IApplicationMigrationEngine"/> that discovers and executes application migrations.
/// </summary>
/// <typeparam name="T">
/// The type of the migration engine. Must inherit from <see cref="BaseMigrationEngine"/>.
/// Migrations are discovered from the assembly containing this type.
/// </typeparam>
/// <remarks>
/// <para>
/// This engine automatically discovers all <see cref="BaseMigration"/> implementations in the assembly
/// containing type <typeparamref name="T"/> and executes them in version order.
/// </para>
/// <para>
/// When a <see cref="DbContext"/> is configured via <see cref="ApplicationMigrationsOptions"/>,
/// each migration is wrapped in a database transaction for atomicity.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationMigrationEngine{T}"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider used to resolve dependencies for migrations.</param>
/// <param name="options">The migration options containing configuration such as the DbContext type.</param>
/// <param name="logger">The logger for recording migration progress and errors.</param>
public class ApplicationMigrationEngine<T>(
    IServiceProvider serviceProvider,
    ApplicationMigrationsOptions options,
    ILogger<IApplicationMigrationEngine> logger
) : IApplicationMigrationEngine where T : BaseMigrationEngine
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private BaseMigration[] _applicationMigrations = [];

    private volatile bool _hasRun;

    /// <inheritdoc />
    public bool HasRun => _hasRun;

    /// <inheritdoc />
    public void Run() => Run(new UseMigrationsOptions());

    /// <inheritdoc />
    public void Run(UseMigrationsOptions runtimeOptions) => RunAsync(runtimeOptions).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task RunAsync() => RunAsync(new UseMigrationsOptions());

    /// <inheritdoc />
    public async Task RunAsync(UseMigrationsOptions runtimeOptions)
    {
        if (_hasRun)
        {
            return;
        }

        await _semaphore.WaitAsync();

        try
        {
            if (_hasRun)
            {
                return;
            }

            using var scope = serviceProvider.CreateScope();

            if (ActivatorUtilities.CreateInstance(scope.ServiceProvider, options.MigrationEngine) is not BaseMigrationEngine engine)
            {
                logger.LogError("No migration engine defined");

                throw new InvalidOperationException($"The type '{options.MigrationEngine.Name}' must inherit from BaseMigrationEngine.");
            }
            else
            {
                if (await engine.ShouldRunAsync())
                {
                    PopulateApplicationMigrations(scope, engine);

                    await engine.RunBeforeAsync();

                    await ApplyMigrationsAsync(engine, scope, runtimeOptions);

                    await engine.RunAfterAsync();
                }
                else
                {
                    logger.LogDebug("Application migrations are configured not to run");
                }

                _hasRun = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Applies pending migrations within the specified version range.
    /// </summary>
    /// <param name="engine">The migration engine providing version tracking.</param>
    /// <param name="scope">The service scope for resolving scoped dependencies.</param>
    /// <param name="runtimeOptions">The runtime options controlling migration behavior.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ApplyMigrationsAsync(BaseMigrationEngine engine, IServiceScope scope, UseMigrationsOptions runtimeOptions)
    {
        var applied = (await engine.GetAppliedVersionsAsync()).OrderBy(v => v);
        var target = _applicationMigrations.LastOrDefault()?.Version ?? new Version(0, 0, 0);
        var current = applied.LastOrDefault() ?? new Version(0, 0, 0);

        if (current <= target)
        {
            var dbContext = ResolveDbContext(scope);

            // Determine which application migrations will run
            // When EnforceLatestMigration is true, re-execute the current version migration (>= current)
            // When EnforceLatestMigration is false (default), only run new migrations (> current)
            var pendingAppMigrations = _applicationMigrations
                .Where(m => runtimeOptions.EnforceLatestMigration ? m.Version >= current : m.Version > current)
                .Where(m => m.Version <= target)
                .OrderBy(m => m.Version)
                .ToList();

            // Set FirstTime and create isolated Cache for each migration
            foreach (var migration in pendingAppMigrations)
            {
                migration.FirstTime = !applied.Any(v => v == migration.Version);
                migration.Cache = new Dictionary<string, object>();
            }

            if (dbContext is not null)
            {
                var pendingEfMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                if (pendingEfMigrations.Any())
                {
                    logger.LogDebug("Found {Count} pending EF Core migrations, executing pre-migration hooks", pendingEfMigrations.Count());

                    // Global engine hook
                    await engine.RunBeforeDatabaseMigrationAsync();

                    // Per-migration hooks
                    foreach (var migration in pendingAppMigrations)
                    {
                        logger.LogDebug("Calling PrepareMigrationAsync for version {Version}", migration.Version);

                        await migration.PrepareMigrationAsync(migration.Cache);
                    }
                }

                logger.LogInformation("Applying Entity Framework Core migrations...");

                await engine.RunEFCoreMigrationAsync(dbContext);

                logger.LogInformation("Entity Framework Core migrations applied");

                await engine.RunAfterDatabaseMigrationAsync();
            }

            foreach (var migration in pendingAppMigrations)
            {
                logger.LogInformation("Applying version {Version}", migration.Version);

                if (dbContext is not null)
                {
                    using var transaction = await dbContext.Database.BeginTransactionAsync();

                    await migration.UpAsync();

                    await transaction.CommitAsync();
                }
                else
                {
                    await migration.UpAsync();
                }

                logger.LogInformation("Version {Version} applied", migration.Version);

                if (migration.Version != current)
                {
                    logger.LogInformation("Registering version {Version}...", migration.Version);

                    await engine.RegisterVersionAsync(migration.Version);

                    logger.LogInformation("Version {Version} registered", migration.Version);
                }
            }
        }
    }

    /// <summary>
    /// Resolves the DbContext from the service provider if configured.
    /// </summary>
    /// <param name="scope">The service scope for resolving the DbContext.</param>
    /// <returns>The resolved DbContext, or null if not configured.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a DbContext type is configured but cannot be resolved from the service provider.
    /// </exception>
    private DbContext? ResolveDbContext(IServiceScope scope)
    {
        if (options.DbContext is null)
        {
            return null;
        }

        var service = scope.ServiceProvider.GetService(options.DbContext);

        if (service is null)
        {
            throw new InvalidOperationException(
                $"The DbContext type '{options.DbContext.Name}' is configured for migrations but is not registered in the service provider. " +
                $"Ensure that '{options.DbContext.Name}' is added to the service collection before calling AddApplicationMigrations.");
        }

        if (service is not DbContext dbContext)
        {
            throw new InvalidOperationException(
                $"The type '{options.DbContext.Name}' is configured as a DbContext but does not inherit from DbContext.");
        }

        return dbContext;
    }

    /// <summary>
    /// Determines whether a type inherits from the specified base type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="baseType">The base type to look for in the inheritance chain.</param>
    /// <returns><c>true</c> if <paramref name="type"/> inherits from <paramref name="baseType"/>; otherwise, <c>false</c>.</returns>
    private static bool IsInheritingFrom(Type type, Type baseType)
    {
        while (type.BaseType is not null)
        {
            if (type.BaseType == baseType)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Discovers and instantiates all migration classes from the configured assembly.
    /// </summary>
    /// <param name="scope">The service scope for resolving migration dependencies.</param>
    /// <param name="engine">The migration engine (used as fallback if no assembly is configured).</param>
    private void PopulateApplicationMigrations(IServiceScope scope, BaseMigrationEngine engine)
    {
        var assembly = options.MigrationsAssembly ?? engine.GetType().Assembly;

        _applicationMigrations = assembly
            .GetTypes()
            .Where(t => IsInheritingFrom(t, typeof(BaseMigration)) && !t.IsAbstract)
            .Select(t => ActivatorUtilities.CreateInstance(scope.ServiceProvider, t))
            .OfType<BaseMigration>()
            .OrderBy(t => t.Version)
            .ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}