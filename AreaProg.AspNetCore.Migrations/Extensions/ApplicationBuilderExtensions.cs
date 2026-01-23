namespace AreaProg.AspNetCore.Migrations.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using AreaProg.Migrations.Interfaces;
using AreaProg.Migrations.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to run application migrations.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Executes pending application migrations synchronously.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseMigrations();
    /// app.Run();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseMigrations(this IApplicationBuilder app) => app.UseMigrations(_ => { });

    /// <summary>
    /// Executes pending application migrations synchronously with the specified options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">A delegate to configure the migration options.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseMigrations(opts =>
    /// {
    ///     opts.EnforceLatestMigration = env.IsDevelopment();
    /// });
    /// app.Run();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseMigrations(this IApplicationBuilder app, Action<UseMigrationsOptions> configure)
    {
        var migrationEngine = app.ApplicationServices.GetRequiredService<IApplicationMigrationEngine>();

        var options = new UseMigrationsOptions();

        configure(options);

        migrationEngine.Run(options);

        return app;
    }

    /// <summary>
    /// Executes pending application migrations asynchronously.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>A task representing the asynchronous operation, containing the application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// await app.UseMigrationsAsync();
    /// app.Run();
    /// </code>
    /// </example>
    public static Task<IApplicationBuilder> UseMigrationsAsync(this IApplicationBuilder app) => app.UseMigrationsAsync(_ => { });

    /// <summary>
    /// Executes pending application migrations asynchronously with the specified options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">A delegate to configure the migration options.</param>
    /// <returns>A task representing the asynchronous operation, containing the application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// await app.UseMigrationsAsync(opts =>
    /// {
    ///     opts.EnforceLatestMigration = env.IsDevelopment();
    /// });
    /// app.Run();
    /// </code>
    /// </example>
    public static async Task<IApplicationBuilder> UseMigrationsAsync(this IApplicationBuilder app, Action<UseMigrationsOptions> configure)
    {
        var migrationEngine = app.ApplicationServices.GetRequiredService<IApplicationMigrationEngine>();

        var options = new UseMigrationsOptions();

        configure(options);

        await migrationEngine.RunAsync(options);

        return app;
    }
}
