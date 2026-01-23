namespace AreaProg.Migrations.Engines;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.Extensions;
using System;

/// <summary>
/// A ready-to-use migration engine for SQL Server with distributed locking support.
/// </summary>
/// <remarks>
/// <para>
/// Use this class when you don't need to customize lifecycle hooks or lock settings.
/// Simply register it:
/// </para>
/// <code>
/// builder.Services.AddApplicationMigrations&lt;DefaultSqlServerMigrationEngine, MyDbContext&gt;();
/// </code>
/// <para>
/// This uses the default lock settings:
/// <list type="bullet">
///   <item>Lock resource name: "AppMigrations"</item>
///   <item>Lock timeout: 0ms (no wait - skip if lock is held)</item>
/// </list>
/// </para>
/// <para>
/// If you need custom hooks or lock settings, inherit from <see cref="SqlServerMigrationEngine"/> instead.
/// </para>
/// </remarks>
public sealed class DefaultSqlServerMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider
) : SqlServerMigrationEngine(serviceProvider, options.DbContext);
