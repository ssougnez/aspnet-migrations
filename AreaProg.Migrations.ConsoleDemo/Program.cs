using AreaProg.Migrations.ConsoleDemo.Data;
using AreaProg.Migrations.ConsoleDemo.Migrations;
using AreaProg.Migrations.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("==============================================");
Console.WriteLine("  Console Application Migration Demo");
Console.WriteLine("  Testing IHost.RunMigrationsAsync()");
Console.WriteLine("==============================================");
Console.WriteLine();

// Build the host with DI configuration
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    })
    .ConfigureServices(services =>
    {
        // Configure SQLite database
        services.AddDbContext<ConsoleDbContext>(options =>
        {
            options.UseSqlite("Data Source=console-demo.db");
        });

        // Register application migrations using IHost-compatible extensions
        services.AddApplicationMigrations<ConsoleMigrationEngine, ConsoleDbContext>();
    })
    .Build();

// Run migrations using the new IHost extension method
// This will:
// 1. Apply EF Core migrations (creates database + schema)
// 2. Apply application migrations (seeds data, etc.)
Console.WriteLine("Running migrations via host.RunMigrationsAsync()...");
Console.WriteLine();

await host.RunMigrationsAsync();

// Display the results
Console.WriteLine();
Console.WriteLine("--- Current Settings ---");

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConsoleDbContext>();

    var settings = await db.Settings.ToListAsync();
    foreach (var setting in settings)
    {
        Console.WriteLine($"  {setting.Key} = {setting.Value}");
    }

    Console.WriteLine();
    Console.WriteLine("--- Applied Migrations ---");

    var migrations = await db.AppliedMigrations.OrderBy(m => m.Version).ToListAsync();
    foreach (var migration in migrations)
    {
        Console.WriteLine($"  {migration.Version} (applied: {migration.AppliedAt})");
    }
}

Console.WriteLine();
Console.WriteLine("Demo completed successfully!");
Console.WriteLine("Run again to see re-execution behavior.");
