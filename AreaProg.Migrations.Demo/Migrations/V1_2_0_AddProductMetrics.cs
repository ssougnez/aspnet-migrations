namespace AreaProg.Migrations.Demo.Migrations;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.Demo.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Migration that demonstrates using PrepareMigrationAsync and the Cache property.
/// Data is captured before EF Core migrations and used after in UpAsync.
/// </summary>
public class V1_2_0_AddProductMetrics(AppDbContext dbContext, ILogger<V1_2_0_AddProductMetrics> logger) : BaseMigration
{
    public override Version Version => new(1, 2, 0);

    /// <summary>
    /// Captures data BEFORE EF Core migrations run.
    /// This is useful when schema changes require data transformation.
    /// Each migration has its own isolated cache.
    /// </summary>
    public override async Task PrepareMigrationAsync(IDictionary<string, object> cache)
    {
        logger.LogInformation("Capturing product metrics before schema migration...");

        if (await TableExistsAsync("Products"))
        {
            var productCount = await dbContext.Products.CountAsync();
            cache["ProductCountBeforeMigration"] = productCount;
            logger.LogInformation("Captured product count: {Count}", productCount);
        }
        else
        {
            cache["ProductCountBeforeMigration"] = 0;
            logger.LogDebug("Products table does not exist yet");
        }
    }

    public override async Task UpAsync()
    {
        logger.LogInformation("Running product metrics migration (FirstTime: {FirstTime})", FirstTime);

        // Access data that was captured in PrepareMigrationAsync
        if (Cache.TryGetValue("ProductCountBeforeMigration", out var countObj))
        {
            var productCountBefore = (int)countObj;
            logger.LogInformation(
                "Product count before database migration: {Count}",
                productCountBefore);
        }

        if (FirstTime)
        {
            // Example: Log a summary of the current state
            var totalProducts = dbContext.Products.Count();
            var totalCategories = dbContext.Categories.Count();

            logger.LogInformation(
                "Database state: {ProductCount} products, {CategoryCount} categories",
                totalProducts,
                totalCategories);
        }

        logger.LogInformation("Product metrics migration completed");
        await Task.CompletedTask;
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;

        if (!wasOpen)
        {
            await connection.OpenAsync();
        }

        try
        {
            using var command = connection.CreateCommand();
            // Check if we're using SQL Server or SQLite based on provider name
            var isSqlServer = connection.GetType().Name.Contains("SqlConnection");
            command.CommandText = isSqlServer
                ? $"SELECT OBJECT_ID('{tableName}', 'U')"
                : $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value;
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
    }
}
