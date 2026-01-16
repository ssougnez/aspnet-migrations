using AreaProg.AspNetCore.Migrations.Demo.Data;
using AreaProg.AspNetCore.Migrations.Demo.Data.Entities;
using AreaProg.AspNetCore.Migrations.Abstractions;
using Microsoft.Extensions.Logging;

namespace AreaProg.AspNetCore.Migrations.Demo.Migrations;

/// <summary>
/// Initial migration that seeds the database with sample data.
/// Demonstrates the use of the FirstTime property.
/// </summary>
public class V1_0_0_InitialSetup : BaseMigration
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<V1_0_0_InitialSetup> _logger;

    public V1_0_0_InitialSetup(AppDbContext dbContext, ILogger<V1_0_0_InitialSetup> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override Version Version => new(1, 0, 0);

    public override async Task UpAsync()
    {
        // FirstTime is true only on the first execution of this migration.
        // Use it for operations that should only run once (like seeding data).
        if (FirstTime)
        {
            _logger.LogInformation("Running initial setup migration for the first time");

            // Seed initial products
            var products = new[]
            {
                new Product
                {
                    Name = "Laptop",
                    Description = "High-performance laptop for developers",
                    Price = 1299.99m,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Mechanical Keyboard",
                    Description = "RGB mechanical keyboard with Cherry MX switches",
                    Price = 149.99m,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Monitor",
                    Description = "27-inch 4K monitor",
                    Price = 499.99m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.Products.AddRange(products);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} initial products", products.Length);
        }
        else
        {
            // This runs on subsequent application startups during development.
            // The current version is always re-executed to facilitate iteration.
            _logger.LogInformation("Initial setup migration re-executed (not first time)");
        }

        // Idempotent operations can run every time
        // For example, ensuring configuration is correct
        _logger.LogInformation("Initial setup migration completed");
    }
}
