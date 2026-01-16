namespace AreaProg.AspNetCore.Migrations.Extensions;

using AreaProg.AspNetCore.Migrations.Abstractions;
using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

/// <summary>
/// Configuration options for application migrations.
/// </summary>
public class ApplicationMigrationsOptions
{
    /// <summary>
    /// Gets or sets the Entity Framework Core DbContext type used by the application.
    /// </summary>
    /// <remarks>
    /// When set, the migration engine will:
    /// <list type="bullet">
    ///   <item>Automatically apply EF Core database migrations before application migrations</item>
    ///   <item>Wrap each application migration in a database transaction</item>
    /// </list>
    /// </remarks>
    public Type? DbContext { get; set; }

    /// <summary>
    /// Gets or sets the migration engine type.
    /// </summary>
    /// <remarks>
    /// This type must inherit from <see cref="BaseMigrationEngine"/>.
    /// </remarks>
    public Type MigrationEngine { get; set; } = null!;

    /// <summary>
    /// Gets or sets the assembly to scan for migration classes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When using <c>AddApplicationMigrations&lt;TEngine, TDbContext&gt;()</c>, this defaults to the assembly
    /// containing <c>TDbContext</c>. This is the recommended approach when using built-in engines like
    /// <c>DefaultEfCoreMigrationEngine</c> or <c>DefaultSqlServerMigrationEngine</c>.
    /// </para>
    /// <para>
    /// When using <c>AddApplicationMigrations&lt;TEngine&gt;()</c>, this defaults to the assembly
    /// containing <c>TEngine</c>.
    /// </para>
    /// <para>
    /// Set this property explicitly when your migrations are in a different assembly than your DbContext
    /// or migration engine.
    /// </para>
    /// </remarks>
    public Assembly? MigrationsAssembly { get; set; }
}

/// <summary>
/// Extension methods for registering application migrations in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the application migration engine to the service collection.
    /// </summary>
    /// <typeparam name="TEngine">
    /// The migration engine type. Must inherit from <see cref="BaseMigrationEngine"/>.
    /// Migrations are discovered from the assembly containing this type.
    /// </typeparam>
    /// <typeparam name="TDbContext">
    /// The Entity Framework Core DbContext type used by the application.
    /// </typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddApplicationMigrations&lt;MyMigrationEngine, MyDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddApplicationMigrations<TEngine, TDbContext>(this IServiceCollection services)
        where TEngine : BaseMigrationEngine
        where TDbContext : class
    {
        return services.AddApplicationMigrationsInternal<TEngine>(options =>
        {
            options.DbContext = typeof(TDbContext);
            options.MigrationsAssembly = typeof(TDbContext).Assembly;
        });
    }

    /// <summary>
    /// Adds the application migration engine to the service collection without Entity Framework Core integration.
    /// </summary>
    /// <typeparam name="TEngine">
    /// The migration engine type. Must inherit from <see cref="BaseMigrationEngine"/>.
    /// Migrations are discovered from the assembly containing this type.
    /// </typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this overload when you don't use Entity Framework Core or when your migration engine
    /// handles database migrations independently.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddApplicationMigrations&lt;MyMigrationEngine&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddApplicationMigrations<TEngine>(this IServiceCollection services)
        where TEngine : BaseMigrationEngine
    {
        return services.AddApplicationMigrationsInternal<TEngine>(null);
    }

    /// <summary>
    /// Internal method to add the application migration engine to the service collection.
    /// </summary>
    internal static IServiceCollection AddApplicationMigrationsInternal<T>(this IServiceCollection services, Action<ApplicationMigrationsOptions>? setupAction) where T : BaseMigrationEngine
    {
        var options = new ApplicationMigrationsOptions
        {
            MigrationEngine = typeof(T)
        };

        setupAction?.Invoke(options);

        // Default to MigrationEngine assembly if MigrationsAssembly not explicitly set
        // For built-in engines (DefaultEfCoreMigrationEngine, DefaultSqlServerMigrationEngine),
        // use AddApplicationMigrations<TEngine, TDbContext>() which sets MigrationsAssembly to DbContext's assembly
        options.MigrationsAssembly ??= typeof(T).Assembly;

        services.AddSingleton(options);

        services.AddSingleton<IApplicationMigrationEngine, ApplicationMigrationEngine<T>>();

        return services;
    }
}
