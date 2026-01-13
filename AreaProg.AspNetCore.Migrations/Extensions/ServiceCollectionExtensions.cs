namespace AreaProg.AspNetCore.Migrations.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Models;
using AreaProg.AspNetCore.Migrations.Services;
using System;

/// <summary>
/// Configuration options for application migrations.
/// </summary>
/// <typeparam name="T">The migration engine type, must inherit from <see cref="BaseMigrationEngine"/>.</typeparam>
public class ApplicationMigrationsOptions<T>
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
    public Type DbContext { get; set; }
}

/// <summary>
/// Extension methods for registering application migrations in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
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
    public static IServiceCollection AddApplicationMigrations<T>(this IServiceCollection services, Action<ApplicationMigrationsOptions<T>> setupAction = null)
    {
        var options = new ApplicationMigrationsOptions<T>();

        setupAction?.Invoke(options);

        services.AddSingleton(options);

        services.AddSingleton<IApplicationMigrationEngine, ApplicationMigrationEngine<T>>();

        return services;
    }
}
