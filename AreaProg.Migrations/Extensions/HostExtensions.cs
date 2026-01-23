namespace AreaProg.Migrations.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AreaProg.Migrations.Interfaces;
using AreaProg.Migrations.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="IHost"/> to run application migrations.
/// </summary>
/// <remarks>
/// These extensions allow running migrations in any .NET host, including console applications,
/// worker services, and ASP.NET Core applications. For ASP.NET Core applications, you can also
/// use the AreaProg.AspNetCore.Migrations package which provides IApplicationBuilder extensions.
/// </remarks>
public static class HostExtensions
{
    /// <summary>
    /// Executes pending application migrations synchronously.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>The host for chaining.</returns>
    /// <remarks>
    /// This method is designed for non-ASP.NET Core hosts (console apps, worker services).
    /// For ASP.NET Core applications using WebApplication, use the UseMigrations extension
    /// from the AreaProg.AspNetCore.Migrations package instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var host = Host.CreateDefaultBuilder(args)
    ///     .ConfigureServices(services => services.AddApplicationMigrations&lt;MyEngine&gt;())
    ///     .Build();
    ///
    /// host.RunMigrations();
    /// host.Run();
    /// </code>
    /// </example>
    public static IHost RunMigrations(this IHost host) => host.RunMigrations(_ => { });

    /// <summary>
    /// Executes pending application migrations synchronously with the specified options.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="configure">A delegate to configure the migration options.</param>
    /// <returns>The host for chaining.</returns>
    /// <remarks>
    /// This method is designed for non-ASP.NET Core hosts (console apps, worker services).
    /// For ASP.NET Core applications using WebApplication, use the UseMigrations extension
    /// from the AreaProg.AspNetCore.Migrations package instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var host = Host.CreateDefaultBuilder(args)
    ///     .ConfigureServices(services => services.AddApplicationMigrations&lt;MyEngine&gt;())
    ///     .Build();
    ///
    /// host.RunMigrations(opts =>
    /// {
    ///     opts.EnforceLatestMigration = env.IsDevelopment();
    /// });
    /// host.Run();
    /// </code>
    /// </example>
    public static IHost RunMigrations(this IHost host, Action<UseMigrationsOptions> configure)
    {
        var migrationEngine = host.Services.GetRequiredService<IApplicationMigrationEngine>();

        var options = new UseMigrationsOptions();

        configure(options);

        migrationEngine.Run(options);

        return host;
    }

    /// <summary>
    /// Executes pending application migrations asynchronously.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>A task representing the asynchronous operation, containing the host for chaining.</returns>
    /// <remarks>
    /// This method is designed for non-ASP.NET Core hosts (console apps, worker services).
    /// For ASP.NET Core applications using WebApplication, use the UseMigrationsAsync extension
    /// from the AreaProg.AspNetCore.Migrations package instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var host = Host.CreateDefaultBuilder(args)
    ///     .ConfigureServices(services => services.AddApplicationMigrations&lt;MyEngine&gt;())
    ///     .Build();
    ///
    /// await host.RunMigrationsAsync();
    /// await host.RunAsync();
    /// </code>
    /// </example>
    public static Task<IHost> RunMigrationsAsync(this IHost host) => host.RunMigrationsAsync(_ => { });

    /// <summary>
    /// Executes pending application migrations asynchronously with the specified options.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="configure">A delegate to configure the migration options.</param>
    /// <returns>A task representing the asynchronous operation, containing the host for chaining.</returns>
    /// <remarks>
    /// This method is designed for non-ASP.NET Core hosts (console apps, worker services).
    /// For ASP.NET Core applications using WebApplication, use the UseMigrationsAsync extension
    /// from the AreaProg.AspNetCore.Migrations package instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// var host = Host.CreateDefaultBuilder(args)
    ///     .ConfigureServices(services => services.AddApplicationMigrations&lt;MyEngine&gt;())
    ///     .Build();
    ///
    /// await host.RunMigrationsAsync(opts =>
    /// {
    ///     opts.EnforceLatestMigration = env.IsDevelopment();
    /// });
    /// await host.RunAsync();
    /// </code>
    /// </example>
    public static async Task<IHost> RunMigrationsAsync(this IHost host, Action<UseMigrationsOptions> configure)
    {
        var migrationEngine = host.Services.GetRequiredService<IApplicationMigrationEngine>();

        var options = new UseMigrationsOptions();

        configure(options);

        await migrationEngine.RunAsync(options);

        return host;
    }
}
