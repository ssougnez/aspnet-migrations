namespace AreaProg.AspNetCore.Migrations.Extensions;

using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Models;
using AreaProg.AspNetCore.Migrations.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

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
    /// Migrations are discovered from the assembly containing this type.
    /// </remarks>
    public Type MigrationEngine { get; set; } = null!;
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
    /// services.AddApplicationMigrations&lt;DefaultEfCoreMigrationEngine, MyDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddApplicationMigrations<TEngine, TDbContext>(this IServiceCollection services)
        where TEngine : BaseMigrationEngine
        where TDbContext : class
    {
        return services.AddApplicationMigrations<TEngine>(options =>
        {
            options.DbContext = typeof(TDbContext);
        });
    }

    /// <summary>
    /// Adds the application migration engine to the service collection.
    /// </summary>
    /// <typeparam name="T">
    /// The migration engine type. Must inherit from <see cref="BaseMigrationEngine"/>.
    /// Migrations are discovered from the assembly containing this type.
    /// </typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="setupAction">Optional action to configure migration options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddApplicationMigrations&lt;MyMigrationEngine&gt;(options =>
    /// {
    ///     options.DbContext = typeof(MyDbContext);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddApplicationMigrations<T>(this IServiceCollection services, Action<ApplicationMigrationsOptions>? setupAction = null) where T : BaseMigrationEngine
    {
        var options = new ApplicationMigrationsOptions
        {
            MigrationEngine = typeof(T)
        };

        setupAction?.Invoke(options);

        services.AddSingleton(options);

        services.AddSingleton<IApplicationMigrationEngine, ApplicationMigrationEngine<T>>();

        return services;
    }
}
