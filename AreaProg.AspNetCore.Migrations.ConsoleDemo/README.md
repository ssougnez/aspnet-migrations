# Console Application Demo

This project demonstrates using `AreaProg.AspNetCore.Migrations` in a **console application** (non-ASP.NET Core).

## Key Difference from ASP.NET Core

Instead of `IApplicationBuilder.UseMigrations()`, console apps use `IHost.RunMigrations()`:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDbContext<ConsoleDbContext>(options =>
            options.UseSqlite("Data Source=console-demo.db"));

        services.AddApplicationMigrations<ConsoleMigrationEngine, ConsoleDbContext>();
    })
    .Build();

// Run migrations using IHost extension
// This applies both EF Core migrations AND application migrations
await host.RunMigrationsAsync();

await host.RunAsync();
```

## Running the Demo

```bash
cd AreaProg.AspNetCore.Migrations.ConsoleDemo
dotnet run
```

**First run output:**
1. Applies EF Core migrations (creates database + schema)
2. Applies application migrations 1.0.0 and 1.1.0
3. Seeds initial settings

**Second run output:**
- EF Core migrations: already applied, nothing to do
- Re-executes migration 1.1.0 (current version)
- Demonstrates idempotent migration behavior

## Project Structure

```
ConsoleDemo/
├── Data/
│   ├── ConsoleDbContext.cs          # EF Core DbContext with Settings table
│   └── EFMigrations/                 # EF Core migrations (schema)
│       └── InitialCreate.cs
├── Migrations/
│   ├── ConsoleMigrationEngine.cs    # Custom engine with lifecycle hooks
│   ├── V1_0_0_InitialSetup.cs       # Uses `FirstTime` for seed data
│   └── V1_1_0_AddMoreSettings.cs    # Idempotent upsert pattern
└── Program.cs                        # Host configuration with `RunMigrationsAsync()`
```

## Migration Workflow

The `RunMigrationsAsync()` method executes:

1. **EF Core migrations** - Creates/updates database schema
2. **Application migrations** - Runs versioned code (seeding, data transforms, etc.)

This is the same workflow as ASP.NET Core apps, just using `IHost` instead of `IApplicationBuilder`.
