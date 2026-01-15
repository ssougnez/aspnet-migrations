using AreaProg.AspNetCore.Migrations.Demo.Data;
using AreaProg.AspNetCore.Migrations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AreaProg.AspNetCore.Migrations.Demo.Migrations;

/// <summary>
/// Migration that demonstrates using PrepareMigrationAsync and the Cache property.
/// Data is captured before EF Core migrations and used after in UpAsync.
/// </summary>
public class V1_2_0_AddProductMetrics : BaseMigration
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<V1_2_0_AddProductMetrics> _logger;

    public V1_2_0_AddProductMetrics(AppDbContext dbContext, ILogger<V1_2_0_AddProductMetrics> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override Version Version => new(1, 2, 0);

    /// <summary>
    /// Captures data BEFORE EF Core migrations run.
    /// This is useful when schema changes require data transformation.
    /// Each migration has its own isolated cache.
    /// </summary>
    public override async Task PrepareMigrationAsync(IDictionary<string, object> cache)
    {
        _logger.LogInformation("Capturing product metrics before schema migration...");

        if (await TableExistsAsync("Products"))
        {
            var productCount = await _dbContext.Products.CountAsync();
            cache["ProductCountBeforeMigration"] = productCount;
            _logger.LogInformation("Captured product count: {Count}", productCount);
        }
        else
        {
            cache["ProductCountBeforeMigration"] = 0;
            _logger.LogDebug("Products table does not exist yet");
        }
    }

    public override async Task UpAsync()
    {
        _logger.LogInformation("Running product metrics migration (FirstTime: {FirstTime})", FirstTime);

        // Access data that was captured in PrepareMigrationAsync
        if (Cache.TryGetValue("ProductCountBeforeMigration", out var countObj))
        {
            var productCountBefore = (int)countObj;
            _logger.LogInformation(
                "Product count before database migration: {Count}",
                productCountBefore);
        }

        if (FirstTime)
        {
            // Example: Log a summary of the current state
            var totalProducts = _dbContext.Products.Count();
            var totalCategories = _dbContext.Categories.Count();

            _logger.LogInformation(
                "Database state: {ProductCount} products, {CategoryCount} categories",
                totalProducts,
                totalCategories);
        }

        _logger.LogInformation("Product metrics migration completed");
        await Task.CompletedTask;
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
