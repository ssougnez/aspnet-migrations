# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - Unreleased

### Added

#### `RunEFCoreMigrationAsync` virtual method on `BaseMigrationEngine`

A new virtual method that allows customizing how Entity Framework Core migrations are executed. Override this method in your migration engine to implement custom behavior such as:

- Custom command timeout values
- Per-migration logging
- Custom execution strategies
- Progress reporting

**Default behavior:**
```csharp
public virtual async Task RunEFCoreMigrationAsync(DbContext? dbContext)
{
    if (dbContext is null) return;

    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

            await dbContext.Database.MigrateAsync();
        });
    }
}
```

**Custom implementation example:**
```csharp
public class MyMigrationEngine : EfCoreMigrationEngine
{
    private readonly ILogger<MyMigrationEngine> _logger;

    public MyMigrationEngine(
        ApplicationMigrationsOptions options,
        IServiceProvider serviceProvider,
        ILogger<MyMigrationEngine> logger)
        : base(serviceProvider, options.DbContext)
    {
        _logger = logger;
    }

    public override async Task RunEFCoreMigrationAsync(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        foreach (var migration in pendingMigrations)
        {
            _logger.LogInformation("Applying EF Core migration: {Migration}", migration);
        }

        // Custom timeout
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        await dbContext.Database.MigrateAsync();
    }
}
```

---

## [2.0.0] - 2026-01-16

### Breaking Changes

#### Namespace reorganization

Classes have been moved to more appropriate namespaces:

| Class | Old Namespace | New Namespace |
|-------|---------------|---------------|
| `BaseMigration` | `AreaProg.AspNetCore.Migrations.Models` | `AreaProg.AspNetCore.Migrations.Abstractions` |
| `BaseMigrationEngine` | `AreaProg.AspNetCore.Migrations.Models` | `AreaProg.AspNetCore.Migrations.Abstractions` |
| `EfCoreMigrationEngine` | `AreaProg.AspNetCore.Migrations.Models` | `AreaProg.AspNetCore.Migrations.Abstractions` |
| `SqlServerMigrationEngine` | `AreaProg.AspNetCore.Migrations.Models` | `AreaProg.AspNetCore.Migrations.Abstractions` |
| `DefaultEfCoreMigrationEngine` | `AreaProg.AspNetCore.Migrations.Models` | `AreaProg.AspNetCore.Migrations.Engines` |
| `DefaultSqlServerMigrationEngine` | `AreaProg.AspNetCore.Migrations.Models` | `AreaProg.AspNetCore.Migrations.Engines` |

**Migration:** Update your `using` statements:

```csharp
// Before (v1.x)
using AreaProg.AspNetCore.Migrations.Models;

// After (v2.x)
using AreaProg.AspNetCore.Migrations.Abstractions; // For BaseMigration, BaseMigrationEngine, EfCoreMigrationEngine, SqlServerMigrationEngine
using AreaProg.AspNetCore.Migrations.Engines;      // For DefaultEfCoreMigrationEngine, DefaultSqlServerMigrationEngine
```

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

#### `AddApplicationMigrations` with setup action is no longer public

The `AddApplicationMigrations<TEngine>(Action<ApplicationMigrationsOptions>)` overload has been made internal. Use the generic overloads instead:

**Before (v1.x):**
```csharp
services.AddApplicationMigrations<MyEngine>(options =>
{
    options.DbContext = typeof(MyDbContext);
});
```

**After (v2.x):**
```csharp
// With DbContext integration
services.AddApplicationMigrations<MyEngine, MyDbContext>();

// Without DbContext (no EF Core integration)
services.AddApplicationMigrations<MyEngine>();
```

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

**Configurable properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `LockResourceName` | `"AppMigrations"` | Name of the SQL Server application lock resource. Override to use different lock scopes for different engines. |
| `LockTimeoutMs` | `0` (no wait) | Lock acquisition timeout in milliseconds. `0` = skip if locked, positive value = wait, `-1` = infinite wait (not recommended). |

```csharp
public class AppMigrationEngine : SqlServerMigrationEngine
{
    public AppMigrationEngine(
        ApplicationMigrationsOptions<AppMigrationEngine> options,
        IServiceProvider serviceProvider)
        : base(serviceProvider, options.DbContext) { }

    protected override string LockResourceName => "MyApp_Migrations";
    protected override int LockTimeoutMs => 5000; // Wait up to 5 seconds
}
```

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

#### `MigrationsAssembly` property on `ApplicationMigrationsOptions`

A new property to explicitly specify which assembly to scan for migration classes. This is particularly useful when using built-in engines (`DefaultEfCoreMigrationEngine`, `DefaultSqlServerMigrationEngine`) since these are in the NuGet package assembly, not your application.

**Default behavior:**
- `AddApplicationMigrations<TEngine, TDbContext>()` → scans the `TDbContext` assembly (recommended for built-in engines)
- `AddApplicationMigrations<TEngine>()` → scans the `TEngine` assembly

**Important:** When using built-in engines like `DefaultSqlServerMigrationEngine`, always use the `<TEngine, TDbContext>` overload so migrations are discovered from your application's assembly (via the DbContext).

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

### Migration Guide from v1.x to v2.x

1. **Update `AddApplicationMigrations` registration:**
   ```csharp
   // Before
   services.AddApplicationMigrations<MyEngine>(options =>
   {
       options.DbContext = typeof(MyDbContext);
   });

   // After - with DbContext
   services.AddApplicationMigrations<MyEngine, MyDbContext>();

   // After - without DbContext
   services.AddApplicationMigrations<MyEngine>();
   ```

2. **Update `ShouldRun` to `ShouldRunAsync()`:**
   ```csharp
   // Before
   public override bool ShouldRun => _config.GetValue<bool>("Migrations:Enabled");

   // After
   public override Task<bool> ShouldRunAsync()
       => Task.FromResult(_config.GetValue<bool>("Migrations:Enabled"));
   ```

3. **Update `RunBeforeDatabaseMigrationAsync` signature:**

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

4. **(Optional) Simplify your engine using `EfCoreMigrationEngine`:**

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

5. **(Optional) Add distributed locking for SQL Server:**

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

6. **Add `AppliedMigration` to your DbContext:**

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
