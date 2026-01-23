namespace AreaProg.Migrations.ConsoleDemo.Migrations;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.ConsoleDemo.Data;
using Microsoft.Extensions.Logging;

public class V1_0_0_InitialSetup(ConsoleDbContext db, ILogger<V1_0_0_InitialSetup> logger) : BaseMigration
{
    public override Version Version => new(1, 0, 0);

    public override async Task UpAsync()
    {
        logger.LogInformation("Running migration {Version}...", Version);

        if (FirstTime)
        {
            logger.LogInformation("First time setup - adding default settings");

            db.Settings.Add(new Setting
            {
                Key = "AppName",
                Value = "Console Demo",
                CreatedAt = DateTime.UtcNow
            });

            db.Settings.Add(new Setting
            {
                Key = "Version",
                Value = "1.0.0",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
        else
        {
            logger.LogInformation("Re-execution - skipping seed data");
        }
    }
}
