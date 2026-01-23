namespace AreaProg.Migrations.Demo.Migrations;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.Demo.Data;
using AreaProg.Migrations.Demo.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Migration that adds product categories.
/// Demonstrates idempotent operations that are safe to re-execute.
/// </summary>
public class V1_1_0_AddCategories(AppDbContext dbContext, ILogger<V1_1_0_AddCategories> logger) : BaseMigration
{
    public override Version Version => new(1, 1, 0);

    public override async Task UpAsync()
    {
        logger.LogInformation("Running category setup migration (FirstTime: {FirstTime})", FirstTime);

        // Example of idempotent "upsert" pattern - safe to re-execute
        var categoriesToAdd = new[]
        {
            new { Name = "Electronics", Description = "Electronic devices and accessories" },
            new { Name = "Peripherals", Description = "Computer peripherals and input devices" },
            new { Name = "Displays", Description = "Monitors and display equipment" }
        };

        foreach (var categoryData in categoriesToAdd)
        {
            var existing = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Name == categoryData.Name);

            if (existing == null)
            {
                dbContext.Categories.Add(new Category
                {
                    Name = categoryData.Name,
                    Description = categoryData.Description
                });
                logger.LogInformation("Added category: {Name}", categoryData.Name);
            }
            else
            {
                // Update existing category description if needed
                existing.Description = categoryData.Description;
                logger.LogInformation("Category already exists: {Name}", categoryData.Name);
            }
        }

        await dbContext.SaveChangesAsync();

        // Assign products to categories (idempotent - only updates if not already set)
        if (FirstTime)
        {
            var electronics = await dbContext.Categories.FirstAsync(c => c.Name == "Electronics");
            var peripherals = await dbContext.Categories.FirstAsync(c => c.Name == "Peripherals");
            var displays = await dbContext.Categories.FirstAsync(c => c.Name == "Displays");

            var laptop = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Laptop");
            var keyboard = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Mechanical Keyboard");
            var monitor = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Monitor");

            if (laptop != null) laptop.CategoryId = electronics.Id;
            if (keyboard != null) keyboard.CategoryId = peripherals.Id;
            if (monitor != null) monitor.CategoryId = displays.Id;

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Assigned products to categories");
        }

        logger.LogInformation("Category setup migration completed");
    }
}
