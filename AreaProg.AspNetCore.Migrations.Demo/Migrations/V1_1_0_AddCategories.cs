using AreaProg.AspNetCore.Migrations.Demo.Data;
using AreaProg.AspNetCore.Migrations.Demo.Data.Entities;
using AreaProg.AspNetCore.Migrations.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AreaProg.AspNetCore.Migrations.Demo.Migrations;

/// <summary>
/// Migration that adds product categories.
/// Demonstrates idempotent operations that are safe to re-execute.
/// </summary>
public class V1_1_0_AddCategories : BaseMigration
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<V1_1_0_AddCategories> _logger;

    public V1_1_0_AddCategories(AppDbContext dbContext, ILogger<V1_1_0_AddCategories> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override Version Version => new(1, 1, 0);

    public override async Task UpAsync()
    {
        _logger.LogInformation("Running category setup migration (FirstTime: {FirstTime})", FirstTime);

        // Example of idempotent "upsert" pattern - safe to re-execute
        var categoriesToAdd = new[]
        {
            new { Name = "Electronics", Description = "Electronic devices and accessories" },
            new { Name = "Peripherals", Description = "Computer peripherals and input devices" },
            new { Name = "Displays", Description = "Monitors and display equipment" }
        };

        foreach (var categoryData in categoriesToAdd)
        {
            var existing = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Name == categoryData.Name);

            if (existing == null)
            {
                _dbContext.Categories.Add(new Category
                {
                    Name = categoryData.Name,
                    Description = categoryData.Description
                });
                _logger.LogInformation("Added category: {Name}", categoryData.Name);
            }
            else
            {
                // Update existing category description if needed
                existing.Description = categoryData.Description;
                _logger.LogInformation("Category already exists: {Name}", categoryData.Name);
            }
        }

        await _dbContext.SaveChangesAsync();

        // Assign products to categories (idempotent - only updates if not already set)
        if (FirstTime)
        {
            var electronics = await _dbContext.Categories.FirstAsync(c => c.Name == "Electronics");
            var peripherals = await _dbContext.Categories.FirstAsync(c => c.Name == "Peripherals");
            var displays = await _dbContext.Categories.FirstAsync(c => c.Name == "Displays");

            var laptop = await _dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Laptop");
            var keyboard = await _dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Mechanical Keyboard");
            var monitor = await _dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Monitor");

            if (laptop != null) laptop.CategoryId = electronics.Id;
            if (keyboard != null) keyboard.CategoryId = peripherals.Id;
            if (monitor != null) monitor.CategoryId = displays.Id;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Assigned products to categories");
        }

        _logger.LogInformation("Category setup migration completed");
    }
}
