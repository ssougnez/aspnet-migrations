# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - Unreleased

### Breaking Changes

#### `ShouldRun` property replaced by `ShouldRunAsync()` method

The synchronous `ShouldRun` property on `BaseMigrationEngine` has been replaced with an asynchronous `ShouldRunAsync()` method to support distributed locking scenarios.

**Before (v1.x):**
```csharp
public class MyMigrationEngine : BaseMigrationEngine
{
    public override bool ShouldRun => true;
}
```

**After (v2.x):**
```csharp
public class MyMigrationEngine : BaseMigrationEngine
{
    public override Task<bool> ShouldRunAsync() => Task.FromResult(true);
}
```

#### `RunBeforeDatabaseMigrationAsync` no longer receives a cache parameter

The `cache` parameter has been removed from `RunBeforeDatabaseMigrationAsync`. Data capture before schema changes should now be done in individual migrations using `PrepareMigrationAsync`.

**Before (v1.x):**
```csharp
public override Task RunBeforeDatabaseMigrationAsync(IDictionary<string, object> cache)
{
    // Capture data here
    cache["key"] = value;
    return Task.CompletedTask;
}
```

**After (v2.x):**
```csharp
// In your migration engine - for global setup/logging only
public override Task RunBeforeDatabaseMigrationAsync()
{
    // Global setup, logging, validation
    return Task.CompletedTask;
}

// In your migration - for data capture
public override async Task PrepareMigrationAsync(IDictionary<string, object> cache)
{
    // Capture data specific to this migration
    cache["key"] = await CaptureDataAsync();
}
```

#### Each migration now has its own isolated cache

Previously, all migrations shared a single cache dictionary. Now each migration has its own isolated cache instance, preventing key collisions between migrations.

### Added

#### `AppliedMigration` entity

A built-in entity for tracking applied migrations. Add it to your `DbContext` to use the new `EfCoreMigrationEngine`:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<AppliedMigration> AppliedMigrations { get; set; }
}
```

#### `EfCoreMigrationEngine` abstract class

A ready-to-use migration engine that stores version history using Entity Framework Core. Eliminates boilerplate code for `GetAppliedVersionsAsync()` and `RegisterVersionAsync()`.

**Usage:**
```csharp
public class AppMigrationEngine : EfCoreMigrationEngine
{
    public AppMigrationEngine(
        ApplicationMigrationsOptions<AppMigrationEngine> options,
        IServiceProvider serviceProvider)
        : base(serviceProvider, options.DbContext) { }

    // Optionally override lifecycle hooks
}
```

**Features:**
- Automatic version tracking via `AppliedMigration` entity
- Graceful handling when database/table doesn't exist yet (uses `CanConnect` + try/catch)
- Deduplication of version registration

#### `DefaultEfCoreMigrationEngine` and `DefaultSqlServerMigrationEngine` classes

Ready-to-use concrete implementations for projects that don't need custom lifecycle hooks:

```csharp
// For any database supported by EF Core
builder.Services.AddApplicationMigrations<DefaultEfCoreMigrationEngine, MyDbContext>();

// For SQL Server with distributed locking
builder.Services.AddApplicationMigrations<DefaultSqlServerMigrationEngine, MyDbContext>();
```

No custom engine class required - just register and go.

#### New `AddApplicationMigrations<TEngine, TDbContext>()` overload

A simpler registration API that takes the DbContext as a generic parameter:

```csharp
// Before
builder.Services.AddApplicationMigrations<MyEngine>(options =>
{
    options.DbContext = typeof(MyDbContext);
});

// After
builder.Services.AddApplicationMigrations<MyEngine, MyDbContext>();
```

#### `PrepareMigrationAsync` method on `BaseMigration`

A new per-migration hook for capturing data before EF Core schema changes. Unlike the global `RunBeforeDatabaseMigrationAsync` on the engine, this method is specific to each migration version.

**Usage:**
```csharp
public class Migration_1_2_0 : BaseMigration
{
    public override Version Version => new(1, 2, 0);

    public override async Task PrepareMigrationAsync(IDictionary<string, object> cache)
    {
        // Capture data BEFORE schema change (called before EF Core migrations)
        var oldStatuses = await _db.Database
            .SqlQueryRaw<OldStatus>("SELECT Id, Status FROM Orders")
            .ToListAsync();
        cache["OrderStatuses"] = oldStatuses;
    }

    public override async Task UpAsync()
    {
        // Transform data AFTER schema change
        if (Cache.TryGetValue("OrderStatuses", out var data))
        {
            var oldStatuses = (List<OldStatus>)data;
            // ... transform data
        }
    }
}
```

**Execution order:**
```
1. ShouldRunAsync()
2. RunBeforeAsync()                              ← Engine (global)
3. RunBeforeDatabaseMigrationAsync()             ← Engine (global)
4. For each pending migration:
   └─ PrepareMigrationAsync(cache)               ← Migration (per-version, isolated cache)
5. EF Core MigrateAsync()
6. RunAfterDatabaseMigrationAsync()              ← Engine (global)
7. For each pending migration:
   └─ UpAsync()                                  ← Migration (per-version)
8. RunAfterAsync()                               ← Engine (global)
```

#### `SqlServerMigrationEngine` abstract class

Extends `EfCoreMigrationEngine` with SQL Server distributed locking using `sp_getapplock`. Ensures only one application instance executes migrations at a time in multi-instance deployments.

**Usage:**
```csharp
public class AppMigrationEngine : SqlServerMigrationEngine
{
    public AppMigrationEngine(
        ApplicationMigrationsOptions<AppMigrationEngine> options,
        IServiceProvider serviceProvider)
        : base(serviceProvider, options.DbContext) { }
}
```

**How it works:**
- Acquires an exclusive application lock named `AppMigrations` before running migrations
- Lock is released automatically when the connection/transaction ends
- Other instances wait or skip based on lock timeout configuration

### Migration Guide from v1.x to v2.x

1. **Update `ShouldRun` to `ShouldRunAsync()`:**
   ```csharp
   // Before
   public override bool ShouldRun => _config.GetValue<bool>("Migrations:Enabled");

   // After
   public override Task<bool> ShouldRunAsync()
       => Task.FromResult(_config.GetValue<bool>("Migrations:Enabled"));
   ```

2. **Update `RunBeforeDatabaseMigrationAsync` signature:**

   If you were capturing data in `RunBeforeDatabaseMigrationAsync`, move that logic to `PrepareMigrationAsync` in individual migrations:

   ```csharp
   // Before (v1.x) - in your engine
   public override async Task RunBeforeDatabaseMigrationAsync(IDictionary<string, object> cache)
   {
       cache["data"] = await CaptureDataAsync();
   }

   // After (v2.x) - in your engine (for global setup only)
   public override Task RunBeforeDatabaseMigrationAsync()
   {
       _logger.LogInformation("Starting schema migrations...");
       return Task.CompletedTask;
   }

   // After (v2.x) - in your migration (for data capture)
   public override async Task PrepareMigrationAsync(IDictionary<string, object> cache)
   {
       cache["data"] = await CaptureDataAsync();
   }
   ```

3. **(Optional) Simplify your engine using `EfCoreMigrationEngine`:**

   If you were manually implementing `GetAppliedVersionsAsync()` and `RegisterVersionAsync()` with a database table, you can now inherit from `EfCoreMigrationEngine` instead:

   ```csharp
   // Before (v1.x) - ~80 lines of boilerplate
   public class AppMigrationEngine : BaseMigrationEngine
   {
       private readonly AppDbContext _dbContext;

       public override async Task<Version[]> GetAppliedVersionsAsync()
       {
           // Manual table existence check
           // Manual query
           // Manual version parsing
       }

       public override async Task RegisterVersionAsync(Version version)
       {
           // Manual deduplication
           // Manual insert
       }
   }

   // After (v2.x) - ~5 lines
   public class AppMigrationEngine : EfCoreMigrationEngine
   {
       public AppMigrationEngine(
           ApplicationMigrationsOptions<AppMigrationEngine> options,
           IServiceProvider serviceProvider)
           : base(serviceProvider, options.DbContext) { }
   }
   ```

3. **(Optional) Add distributed locking for SQL Server:**

   Replace `EfCoreMigrationEngine` with `SqlServerMigrationEngine`:

   ```csharp
   public class AppMigrationEngine : SqlServerMigrationEngine
   {
       public AppMigrationEngine(
           ApplicationMigrationsOptions<AppMigrationEngine> options,
           IServiceProvider serviceProvider)
           : base(serviceProvider, options.DbContext) { }
   }
   ```

4. **Add `AppliedMigration` to your DbContext:**

   If using `EfCoreMigrationEngine` or `SqlServerMigrationEngine`:

   ```csharp
   using AreaProg.AspNetCore.Migrations.Models;

   public class AppDbContext : DbContext
   {
       public DbSet<AppliedMigration> AppliedMigrations { get; set; }
   }
   ```

   Then create an EF Core migration:
   ```bash
   dotnet ef migrations add AddAppliedMigrations
   ```

---

## [1.1.0] - Previous Release

### Changed

- Renamed `GetAppliedVersionAsync` to `GetAppliedVersionsAsync` for clarity

---

## [1.0.0] - Initial Release

### Added

- `BaseMigration` abstract class for defining application migrations
- `BaseMigrationEngine` abstract class for implementing version tracking
- `ApplicationMigrationEngine<T>` for automatic migration discovery and execution
- Lifecycle hooks: `RunBeforeAsync`, `RunAfterAsync`, `RunBeforeDatabaseMigrationAsync`, `RunAfterDatabaseMigrationAsync`
- `FirstTime` property to distinguish initial execution from re-runs
- `Cache` dictionary for passing data between hooks and migrations
- EF Core transaction support when `DbContext` is configured
- `UseMigrations()` and `UseMigrationsAsync()` extension methods for `IApplicationBuilder`
