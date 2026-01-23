namespace AreaProg.Migrations.Models;

/// <summary>
/// Configuration options for running application migrations.
/// </summary>
/// <remarks>
/// These options control runtime behavior when executing migrations via
/// UseMigrations or UseMigrationsAsync extension methods on IApplicationBuilder.
/// </remarks>
public class UseMigrationsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to re-execute the migration matching the current registered version.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to <c>true</c>, the migration with the same version as the last applied version will be
    /// re-executed on each application startup. This is useful during development to iterate on migrations without
    /// manually rolling back the database version.
    /// </para>
    /// <para>
    /// When set to <c>false</c> (default), only migrations with versions strictly greater than the current
    /// registered version will be executed. This is the recommended setting for production environments.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable re-execution of current version migration in development
    /// await app.UseMigrationsAsync(opts =>
    /// {
    ///     opts.EnforceLatestMigration = env.IsDevelopment();
    /// });
    /// </code>
    /// </example>
    public bool EnforceLatestMigration { get; set; } = false;
}
