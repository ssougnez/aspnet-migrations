namespace AreaProg.Migrations.Abstractions;

using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading.Tasks;

/// <summary>
/// Migration engine for SQL Server with distributed locking support using <c>sp_getapplock</c>.
/// </summary>
/// <remarks>
/// <para>
/// This engine extends <see cref="EfCoreMigrationEngine"/> with distributed locking capabilities,
/// ensuring that only one application instance can execute migrations at a time in multi-instance deployments.
/// </para>
/// <para>
/// The lock is acquired at the session level and held until the migration process completes
/// and the database connection is closed.
/// </para>
/// <para>
/// Usage:
/// </para>
/// <code>
/// public class AppMigrationEngine : SqlServerMigrationEngine
/// {
///     public AppMigrationEngine(
///         ApplicationMigrationsOptions&lt;AppMigrationEngine&gt; options,
///         IServiceProvider serviceProvider)
///         : base(serviceProvider, options.DbContext) { }
/// }
/// </code>
/// </remarks>
/// <param name="serviceProvider">The service provider to resolve the DbContext from.</param>
/// <param name="dbContextType">
/// The type of DbContext to use, typically from <c>ApplicationMigrationsOptions.DbContext</c>.
/// </param>
public abstract class SqlServerMigrationEngine(IServiceProvider serviceProvider, Type? dbContextType)
    : EfCoreMigrationEngine(serviceProvider, dbContextType)
{
    /// <summary>
    /// Gets the name of the application lock resource.
    /// </summary>
    /// <remarks>
    /// Override this property to use a custom lock name if you need different lock scopes
    /// for different migration engines in the same database.
    /// </remarks>
    protected virtual string LockResourceName => "AppMigrations";

    /// <summary>
    /// Gets the lock timeout in milliseconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is 0 (no wait). If another instance holds the lock, this instance will skip migrations.
    /// </para>
    /// <para>
    /// Override to set a positive value if you want instances to wait for the lock.
    /// Use -1 for infinite wait (not recommended in production).
    /// </para>
    /// </remarks>
    protected virtual int LockTimeoutMs => 0;

    /// <summary>
    /// Determines whether migrations should run by attempting to acquire a distributed lock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses SQL Server's <c>sp_getapplock</c> to acquire an exclusive session-level lock.
    /// If the lock cannot be acquired (another instance is running migrations), returns <c>false</c>.
    /// </para>
    /// <para>
    /// The lock is automatically released when the database connection closes at the end of the scope.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <c>true</c> if the lock was acquired and migrations should proceed;
    /// <c>false</c> if another instance holds the lock or no DbContext is configured.
    /// </returns>
    public override async Task<bool> ShouldRunAsync()
    {
        if (DbContext is null)
        {
            return true;
        }

        var connection = DbContext.Database.GetDbConnection();

        if (connection.State is not ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var command = connection.CreateCommand();

        command.CommandText = @"
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = @resourceName,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = @timeout;
            SELECT @result;";

        var resourceParam = command.CreateParameter();

        resourceParam.ParameterName = "@resourceName";
        resourceParam.Value = LockResourceName;
        resourceParam.DbType = DbType.String;

        command.Parameters.Add(resourceParam);

        var timeoutParam = command.CreateParameter();

        timeoutParam.ParameterName = "@timeout";
        timeoutParam.Value = LockTimeoutMs;
        timeoutParam.DbType = DbType.Int32;

        command.Parameters.Add(timeoutParam);

        var result = await command.ExecuteScalarAsync();

        // sp_getapplock return values:
        // 0: Lock acquired successfully (synchronously)
        // 1: Lock acquired after waiting
        // -1: Timeout (lock not acquired)
        // -2: Lock request was cancelled
        // -3: Deadlock victim
        // -999: Parameter validation error
        return result is not null && Convert.ToInt32(result) >= 0;
    }

    /// <summary>
    /// Called after all migrations complete. Releases the distributed lock.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task RunAfterAsync()
    {
        await ReleaseLockAsync();

        await base.RunAfterAsync();
    }

    private async Task ReleaseLockAsync()
    {
        if (DbContext is null)
        {
            return;
        }

        var connection = DbContext.Database.GetDbConnection();

        if (connection.State is ConnectionState.Open)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                EXEC sp_releaseapplock
                    @Resource = @resourceName,
                    @LockOwner = 'Session';";

            var resourceParam = command.CreateParameter();

            resourceParam.ParameterName = "@resourceName";
            resourceParam.Value = LockResourceName;
            resourceParam.DbType = DbType.String;

            command.Parameters.Add(resourceParam);

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Lock may have already been released or connection closed
                // This is not an error condition
            }
        }
    }
}
