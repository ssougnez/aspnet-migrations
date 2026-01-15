namespace AreaProg.AspNetCore.Migrations.Models;

using AreaProg.AspNetCore.Migrations.Extensions;
using System;

/// <summary>
/// A ready-to-use migration engine that stores version history using Entity Framework Core.
/// </summary>
/// <remarks>
/// <para>
/// Use this class when you don't need to customize lifecycle hooks. Simply register it:
/// </para>
/// <code>
/// builder.Services.AddApplicationMigrations&lt;DefaultEfCoreMigrationEngine, MyDbContext&gt;();
/// </code>
/// <para>
/// If you need custom hooks (e.g., logging, validation), inherit from <see cref="EfCoreMigrationEngine"/> instead.
/// </para>
/// </remarks>
public sealed class DefaultEfCoreMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider
) : EfCoreMigrationEngine(serviceProvider, options.DbContext);
