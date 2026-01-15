using AreaProg.AspNetCore.Migrations.Demo.Data;
using AreaProg.AspNetCore.Migrations.Demo.Data.Entities;
using AreaProg.AspNetCore.Migrations.Demo.Migrations;
using AreaProg.AspNetCore.Migrations.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 1. Configure Entity Framework Core with SQLite
// =============================================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=demo.db"));

// =============================================================================
// 2. Register Application Migrations
// =============================================================================
// AddApplicationMigrations<T> registers the migration engine.
// The type parameter T is used to discover migrations in the same assembly.
// The DbContext option enables transactional migrations and EF Core integration.
builder.Services.AddApplicationMigrations<AppMigrationEngine>(options =>
{
    options.DbContext = typeof(AppDbContext);
});

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =============================================================================
// 3. Run Migrations at Startup
// =============================================================================
// UseMigrationsAsync() executes:
// - RunBeforeAsync() hook
// - RunBeforeDatabaseMigrationAsync() hook (if pending EF migrations)
// - EF Core database migrations (context.Database.Migrate())
// - RunAfterDatabaseMigrationAsync() hook (if EF migrations ran)
// - Application migrations (BaseMigration.UpAsync() for each pending version)
// - RunAfterAsync() hook
await app.UseMigrationsAsync();

// Configure Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// =============================================================================
// API Endpoints
// =============================================================================

// GET /products - List all products with their categories
app.MapGet("/products", async (AppDbContext db) =>
{
    var products = await db.Products
        .Include(p => p.Category)
        .Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            Category = p.Category != null ? p.Category.Name : null,
            p.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

// GET /categories - List all categories with product count
app.MapGet("/categories", async (AppDbContext db) =>
{
    var categories = await db.Categories
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.Description,
            ProductCount = c.Products.Count
        })
        .ToListAsync();

    return Results.Ok(categories);
})
.WithName("GetCategories")
.WithOpenApi();

// POST /products - Create a new product
app.MapPost("/products", async (AppDbContext db, ProductCreateRequest request) =>
{
    var product = new Product
    {
        Name = request.Name,
        Description = request.Description,
        Price = request.Price,
        CategoryId = request.CategoryId,
        CreatedAt = DateTime.UtcNow
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

// GET /migrations - Show migration history
app.MapGet("/migrations", async (AppDbContext db) =>
{
    var migrations = await db.AppliedMigrations
        .OrderByDescending(m => m.AppliedAt)
        .Select(m => new
        {
            m.Version,
            m.AppliedAt
        })
        .ToListAsync();

    return Results.Ok(migrations);
})
.WithName("GetMigrations")
.WithOpenApi();

app.Run();

// =============================================================================
// Request/Response Models
// =============================================================================
record ProductCreateRequest(string Name, string? Description, decimal Price, int? CategoryId);
