namespace AreaProg.AspNetCore.Migrations.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AreaProg.AspNetCore.Migrations.Extensions;
using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Models;
using System;
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
/// When a <see cref="DbContext"/> is configured via <see cref="ApplicationMigrationsOptions{T}"/>,
/// each migration is wrapped in a database transaction for atomicity.
/// </para>
/// </remarks>
public class ApplicationMigrationEngine<T> : IApplicationMigrationEngine
{
    private readonly ApplicationMigrationsOptions<T> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IApplicationMigrationEngine> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private BaseMigration[] _applicationMigrations = null;
    private volatile bool _hasRun;

    /// <inheritdoc />
    public bool HasRun => _hasRun;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMigrationEngine{T}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies for migrations.</param>
    /// <param name="options">The migration options containing configuration such as the DbContext type.</param>
    /// <param name="logger">The logger for recording migration progress and errors.</param>
    public ApplicationMigrationEngine(IServiceProvider serviceProvider, ApplicationMigrationsOptions<T> options, ILogger<IApplicationMigrationEngine> logger)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Run() => RunAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task RunAsync()
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

            using var scope = _serviceProvider.CreateScope();

            if (ActivatorUtilities.CreateInstance(scope.ServiceProvider, _options.GetType().GetGenericArguments()[0].UnderlyingSystemType) is not BaseMigrationEngine engine)
            {
                _logger.LogError("No migration engine defined");

                throw new InvalidOperationException($"The type '{_options.GetType().GetGenericArguments()[0].Name}' must inherit from BaseMigrationEngine.");
            }
            else
            {
                if (engine.ShouldRun)
                {
                    PopulateApplicationMigrations(scope, engine);

                    await engine.RunBeforeAsync();

                    await ApplyMigrationsAsync(engine, scope);

                    await engine.RunAfterAsync();
                }
                else
                {
                    _logger.LogDebug("Application migrations are configured not to run");
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
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ApplyMigrationsAsync(BaseMigrationEngine engine, IServiceScope scope)
    {
        var applied = (await engine.GetAppliedVersionAsync()).OrderBy(v => v);
        var target = _applicationMigrations.LastOrDefault()?.Version ?? new Version(0, 0, 0);
        var current = applied.LastOrDefault() ?? new Version(0, 0, 0);

        if (current <= target)
        {
            var dbContext = _options.DbContext != null ? scope.ServiceProvider.GetService(_options.DbContext) as DbContext : null;

            if (dbContext != null)
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();

                _logger.LogInformation("Applying Entity Framework Core migrations...");

                await strategy.ExecuteAsync(async () =>
                {
                    dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

                    await using var transaction = await dbContext.Database.BeginTransactionAsync();

                    await dbContext.Database.MigrateAsync();

                    await transaction.CommitAsync();
                });

                _logger.LogInformation("Entity Framework Core migrations applied");

                await engine.RunAfterDatabaseMigration();
            }

            var migrations = _applicationMigrations
                .Where(m => m.Version >= current && m.Version <= target)
                .OrderBy(m => m.Version);

            foreach (var migration in migrations)
            {
                _logger.LogInformation("Applying version {Version}", migration.Version);

                migration.FirstTime = !applied.Any(v => v == migration.Version);

                if (dbContext != null)
                {
                    using var transaction = await dbContext.Database.BeginTransactionAsync();

                    await migration.UpAsync();

                    await transaction.CommitAsync();
                }
                else
                {
                    await migration.UpAsync();
                }

                _logger.LogInformation("Version {Version} applied", migration.Version);

                if (migration.Version != current)
                {
                    _logger.LogInformation("Registering version {Version}...", migration.Version);

                    await engine.RegisterVersionAsync(migration.Version);

                    _logger.LogInformation("Version {Version} registered", migration.Version);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a type inherits from the specified base type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="baseType">The base type to look for in the inheritance chain.</param>
    /// <returns><c>true</c> if <paramref name="type"/> inherits from <paramref name="baseType"/>; otherwise, <c>false</c>.</returns>
    private bool IsInheritingFrom(Type type, Type baseType)
    {
        while (type.BaseType != null)
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
    /// Discovers and instantiates all migration classes from the engine's assembly.
    /// </summary>
    /// <param name="scope">The service scope for resolving migration dependencies.</param>
    /// <param name="engine">The migration engine whose assembly will be scanned for migrations.</param>
    private void PopulateApplicationMigrations(IServiceScope scope, BaseMigrationEngine engine)
    {
        _applicationMigrations = engine
            .GetType()
            .Assembly
            .GetTypes()
            .Where(t => IsInheritingFrom(t, typeof(BaseMigration)) && t.IsAbstract == false)
            .Select(t => ActivatorUtilities.CreateInstance(scope.ServiceProvider, t) as BaseMigration)
            .OrderBy(t => t.Version)
            .ToArray();
    }
}