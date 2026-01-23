namespace AreaProg.AspNetCore.Migrations.ConsoleDemo.Migrations;

using AreaProg.AspNetCore.Migrations.Abstractions;
using AreaProg.AspNetCore.Migrations.Extensions;
using Microsoft.Extensions.Logging;

public class ConsoleMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider,
    ILogger<ConsoleMigrationEngine> logger
) : EfCoreMigrationEngine(serviceProvider, options.DbContext)
{
    public override Task RunBeforeAsync()
    {
        logger.LogInformation("=== Starting migrations ===");

        return Task.CompletedTask;
    }

    public override Task RunAfterAsync()
    {
        logger.LogInformation("=== Migrations completed ===");

        return Task.CompletedTask;
    }
}
