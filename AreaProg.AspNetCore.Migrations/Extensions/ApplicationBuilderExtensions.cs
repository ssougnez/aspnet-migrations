namespace AreaProg.AspNetCore.Migrations.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using AreaProg.AspNetCore.Migrations.Interfaces;
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
    public static IApplicationBuilder UseMigrations(this IApplicationBuilder app)
    {
        var migrationEngine = app.ApplicationServices.GetRequiredService<IApplicationMigrationEngine>();

        migrationEngine.Run();

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
    public static async Task<IApplicationBuilder> UseMigrationsAsync(this IApplicationBuilder app)
    {
        var migrationEngine = app.ApplicationServices.GetRequiredService<IApplicationMigrationEngine>();

        await migrationEngine.RunAsync();

        return app;
    }
}
