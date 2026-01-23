namespace AreaProg.Migrations.ConsoleDemo.Migrations;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.ConsoleDemo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class V1_1_0_AddMoreSettings(ConsoleDbContext db, ILogger<V1_1_0_AddMoreSettings> logger) : BaseMigration
{
    public override Version Version => new(1, 1, 0);

    public override async Task UpAsync()
    {
        logger.LogInformation("Running migration {Version}...", Version);

        // Idempotent upsert - safe to run multiple times
        var existing = await db.Settings.FirstOrDefaultAsync(s => s.Key == "Environment");

        if (existing == null)
        {
            logger.LogInformation("Adding Environment setting");
            db.Settings.Add(new Setting
            {
                Key = "Environment",
                Value = "Development",
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            logger.LogInformation("Environment setting already exists, skipping");
        }

        // Update version setting
        var versionSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "Version");
        if (versionSetting != null)
        {
            versionSetting.Value = "1.1.0";
            logger.LogInformation("Updated Version setting to 1.1.0");
        }

        await db.SaveChangesAsync();
    }
}
