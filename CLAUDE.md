# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AreaProg.AspNetCore.Migrations is a .NET class library (NuGet package) that provides application-level migration infrastructure for ASP.NET Core applications. It complements Entity Framework Core database migrations by enabling versioned application migrations with lifecycle hooks.

## Build Commands

```bash
# Build the solution
dotnet build

# Build for a specific framework
dotnet build -f net8.0

# Create NuGet package (auto-generated on build)
dotnet pack
```

The project multi-targets: net6.0, net8.0, net9.0, net10.0.

## Architecture

### Core Pattern

The library uses an abstract base class pattern for extensibility:

1. **BaseMigration** (`Models/BaseMigration.cs`) - Abstract class that individual migrations inherit from. Requires implementing `Version` property and `UpAsync()` method.

2. **BaseMigrationEngine** (`Models/BaseMigrationEngine.cs`) - Abstract orchestration layer that tracks applied versions. Subclasses implement version storage (e.g., database, file system).

3. **ApplicationMigrationEngine<T>** (`Services/ApplicationMigrationEngine.cs`) - Generic concrete implementation that:
   - Discovers `BaseMigration` implementations via reflection from assembly containing type `T`
   - Executes migrations sequentially based on version ordering
   - Wraps migrations in EF Core transactions when DbContext is configured
   - Provides lifecycle hooks: `RunBeforeAsync()`, `RunAfterAsync()`, `RunAfterDatabaseMigration()`

### DI Registration

```csharp
services.AddApplicationMigrations<MyMigrationEngine>(options =>
{
    options.DbContext = typeof(MyDbContext); // Optional: enables transactional migrations
});
```

### Running Migrations

Use the extension methods on `IApplicationBuilder` to run migrations at startup:

```csharp
var app = builder.Build();

// Synchronous
app.UseMigrations();

// Or asynchronous
await app.UseMigrationsAsync();

app.Run();
```

### Public Interface

`IApplicationMigrationEngine` exposes:
- `Run()` - Executes pending migrations synchronously
- `RunAsync()` - Executes pending migrations asynchronously
- `HasRun` - Indicates if migrations have executed

## Key Implementation Details

- Migrations are discovered at runtime via reflection scanning the assembly containing the engine type
- Version comparison uses `System.Version` ordering - migrations with higher versions run later
- EF Core database migrations run automatically via `context.Database.Migrate()` when DbContext is configured

### Current Version Re-execution (Development Workflow)

The migration matching the current registered version is **re-executed on each application startup**. This is intentional to facilitate development:

- You can iterate on a migration without manually rolling back the database version
- No need to delete version records or reset state between debugging sessions

To handle re-execution, use one of these strategies:

1. **Use `FirstTime` property** - Guards operations that should only run once:
   ```csharp
   public override async Task UpAsync()
   {
       if (FirstTime)
       {
           // Runs only on first installation (e.g., seed data, notifications)
       }

       // Idempotent operations run every time
   }
   ```

2. **Design idempotent migrations** - Methods that are safe to re-execute (upserts, "create if not exists", etc.)

The `FirstTime` flag is `true` when the migration version has never been registered, `false` on re-executions. During debugging, you can bypass the `FirstTime` check by moving the execution pointer.
