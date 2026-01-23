# AreaProg.AspNetCore.Migrations

Application-level migrations for ASP.NET Core. Run versioned code at startup, complementing Entity Framework Core database migrations.

## Why use this?

EF Core migrations handle **database schema changes**. But what about:
- Seeding initial data
- Transforming existing data after schema changes
- Running one-time setup code (creating folders, sending notifications, etc.)
- Applying configuration changes across releases

This library provides **application migrations** - versioned code that runs once per version, with full dependency injection support.

## Installation

```bash
dotnet add package AreaProg.AspNetCore.Migrations
```

> **Upgrading from v1.x?** See the [CHANGELOG.md](CHANGELOG.md) for breaking changes and migration guide.

## Quick Start

### 1. Add the AppliedMigration Entity to Your DbContext

The library provides an `AppliedMigration` entity for tracking versions:

```csharp
using AreaProg.AspNetCore.Migrations.Models;

public class MyDbContext : DbContext
{
    public DbSet<AppliedMigration> AppliedMigrations { get; set; }

    // ... your other DbSets
}
```

### 2. Register a Migration Engine

**Option A: Use the built-in engine (simplest)**

No custom class needed:

```csharp
builder.Services.AddApplicationMigrations<DefaultEfCoreMigrationEngine, MyDbContext>();
```

**Option B: Create a custom engine (for lifecycle hooks)**

```csharp
using AreaProg.AspNetCore.Migrations.Abstractions;
using AreaProg.AspNetCore.Migrations.Extensions;

public class MyMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider
) : EfCoreMigrationEngine(serviceProvider, options.DbContext)
{
    public override Task RunBeforeAsync()
    {
        // Custom logic before migrations
        return Task.CompletedTask;
    }
}
```

The base class handles `GetAppliedVersionsAsync()` and `RegisterVersionAsync()` automatically (see [Custom Version Storage](#advanced-custom-version-storage) for details).

### 3. Create a Migration

```csharp
public class Migration_1_0_0(MyDbContext db) : BaseMigration
{
    public override Version Version => new(1, 0, 0);

    public override async Task UpAsync()
    {
        // Your migration code here
        await db.Users.AddAsync(new User { Name = "Admin", IsAdmin = true });
        await db.SaveChangesAsync();
    }
}
```

### 4. Register and Run

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(...);

builder.Services.AddApplicationMigrations<MyMigrationEngine, MyDbContext>();

var app = builder.Build();

app.UseMigrations();

app.Run();
```

That's it! At startup:
1. EF Core database migrations run automatically
2. Each application migration runs in a transaction
3. Versions are tracked in your database

## Multi-Instance Deployments (SQL Server)

When running multiple application instances (load-balanced, Kubernetes, etc.), you need to ensure only **one instance** runs migrations at a time.

### Using SqlServerMigrationEngine

For SQL Server, use `DefaultSqlServerMigrationEngine` or inherit from `SqlServerMigrationEngine`. It uses `sp_getapplock` for distributed locking:

**Option A: Use the built-in engine (simplest)**

```csharp
builder.Services.AddApplicationMigrations<DefaultSqlServerMigrationEngine, MyDbContext>();
```

**Option B: Create a custom engine (for custom lock settings or hooks)**

```csharp
using AreaProg.AspNetCore.Migrations.Abstractions;
using AreaProg.AspNetCore.Migrations.Extensions;

public class MyMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider
) : SqlServerMigrationEngine(serviceProvider, options.DbContext)
{
}
```

**How it works:**
- Before running migrations, acquires an exclusive lock named `AppMigrations`
- If another instance holds the lock, this instance skips migrations
- Lock is released automatically after migrations complete

**Customization (requires custom engine):**

```csharp
public class MyMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider
) : SqlServerMigrationEngine(serviceProvider, options.DbContext)
{
    // Custom lock name (default: "AppMigrations")
    protected override string LockResourceName => "MyApp_Migrations";

    // Lock timeout in ms (default: 0 = no wait)
    // Set to positive value to wait for the lock
    protected override int LockTimeoutMs => 5000;
}
```

### Manual Approach (Other Databases)

For non-SQL Server databases, use configuration to designate a "master" instance:

```json
// appsettings.json (on master instance only)
{
  "Migrations": {
    "Enabled": true
  }
}
```

```csharp
public class MyMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider,
    IConfiguration configuration
) : EfCoreMigrationEngine(serviceProvider, options.DbContext)
{
    public override Task<bool> ShouldRunAsync()
        => Task.FromResult(configuration.GetValue("Migrations:Enabled", false));
}
```

### Using Redis (Redlock)

For distributed locking across any database type, you can use Redis with the [Redlock algorithm](https://redis.io/docs/manual/patterns/distributed-locks/). Override `ShouldRunAsync()` to acquire a distributed lock:

```csharp
public class MyMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider,
    IDistributedLockFactory lockFactory  // From RedLock.net or similar
) : EfCoreMigrationEngine(serviceProvider, options.DbContext)
{
    private IRedLock? _lock;

    public override async Task<bool> ShouldRunAsync()
    {
        _lock = await lockFactory.CreateLockAsync(
            "app-migrations",
            expiryTime: TimeSpan.FromMinutes(5),
            waitTime: TimeSpan.Zero,      // Don't wait, skip if locked
            retryTime: TimeSpan.FromMilliseconds(100));

        return _lock.IsAcquired;
    }

    public override async Task RunAfterAsync()
    {
        if (_lock != null)
            await _lock.DisposeAsync();
    }
}
```

This approach works with any database (PostgreSQL, MySQL, SQLite, etc.) as long as you have Redis available.

## Using Dependency Injection

Migrations support constructor injection:

```csharp
public class Migration_1_1_0(MyDbContext db, IEmailService email) : BaseMigration
{
    public override Version Version => new(1, 1, 0);

    public override async Task UpAsync()
    {
        var admins = await db.Users.Where(u => u.IsAdmin).ToListAsync();

        foreach (var admin in admins)
        {
            await email.SendAsync(admin.Email, "System upgraded to 1.1.0");
        }
    }
}
```

## Controlling Re-execution with `EnforceLatestMigration`

By default, the migration matching the current registered version is **re-executed on each application startup**. This facilitates development workflows:

- Iterate on a migration without manually rolling back the database version
- No need to delete version records or reset state between debugging sessions
- Test your migration logic repeatedly until it works correctly

### Disabling Re-execution in Production

For production environments, you can disable this behavior using `EnforceLatestMigration`:

```csharp
await app.UseMigrationsAsync(opts =>
{
    opts.EnforceLatestMigration = env.IsDevelopment();
});
```

| `EnforceLatestMigration` | Behavior |
|--------------------------|----------|
| `true` (default) | Re-executes current version migration (`>= current`) |
| `false` | Only runs new migrations (`> current`) |

**Benefits of `EnforceLatestMigration = false` in production:**
- Faster startup (skips unnecessary re-execution)
- Cleaner logs (no repeated "Applying version X.Y.Z" messages)
- Makes re-execution an intentional development choice

## The `FirstTime` Property

When a migration is re-executed (with `EnforceLatestMigration = true`), use the `FirstTime` property to distinguish between first-time execution and re-execution.

### Handling Re-execution

**Strategy 1: Use the `FirstTime` property**

Guard operations that should only run once:

```csharp
public override async Task UpAsync()
{
    if (FirstTime)
    {
        // Only runs on first installation
        await SeedInitialDataAsync();
        await SendDeploymentNotificationAsync();
    }

    // Code outside the check runs every time
}
```

**Strategy 2: Design idempotent migrations**

Write methods that are safe to re-execute (they produce the same result regardless of how many times they run):

```csharp
public override async Task UpAsync()
{
    // Upsert pattern - safe to run multiple times
    var existing = await _db.Settings.FirstOrDefaultAsync(s => s.Key == "AppVersion");
    if (existing == null)
    {
        _db.Settings.Add(new Setting { Key = "AppVersion", Value = "1.0.0" });
    }
    else
    {
        existing.Value = "1.0.0";
    }
    await _db.SaveChangesAsync();
}
```

**Combining both strategies:**

```csharp
public override async Task UpAsync()
{
    if (FirstTime)
    {
        // One-time operations: data inserts, notifications, etc.
        await SeedInitialDataAsync();
        await SendDeploymentNotificationAsync();
    }

    // Idempotent operations can run every time safely
    await EnsureDefaultSettingsExistAsync();
}
```

The `FirstTime` property is `true` when the migration version has never been registered, and `false` on subsequent re-executions.

## Lifecycle Hooks

Override these methods in your engine for custom behavior:

```csharp
public class MyMigrationEngine(
    ApplicationMigrationsOptions options,
    IServiceProvider serviceProvider
) : EfCoreMigrationEngine(serviceProvider, options.DbContext)
{
    // Called before any migrations
    public override Task RunBeforeAsync() { ... }

    // Called before EF Core migrations (only if migrations are pending)
    public override Task RunBeforeDatabaseMigrationAsync() { ... }

    // Customize how EF Core migrations are executed
    public override Task RunEFCoreMigrationAsync(DbContext? dbContext) { ... }

    // Called after EF Core migrations
    public override Task RunAfterDatabaseMigrationAsync() { ... }

    // Called after all application migrations
    public override Task RunAfterAsync() { ... }
}
```

### Execution Order

```
1. ShouldRunAsync()                              → returns false? skip everything
2. RunBeforeAsync()                              ← Engine (global)
3. RunBeforeDatabaseMigrationAsync()             ← Engine (global) [if EF Core migrations pending]
4. For each pending migration:
   └─ PrepareMigrationAsync(cache)               ← Migration (per-version, isolated cache)
5. RunEFCoreMigrationAsync(dbContext)            ← Engine (customizable EF Core migration execution)
6. RunAfterDatabaseMigrationAsync()              ← Engine (global)
7. For each pending migration:
   └─ UpAsync()                                  ← Migration (per-version)
8. RunAfterAsync()                               ← Engine (global)
```

## Advanced: Data Transformation During Schema Changes

When changing column types (e.g., `int` enum to `string`), you need to capture data before the schema change. Use `PrepareMigrationAsync` and the `Cache`:

```csharp
public class Migration_1_2_0(MyDbContext db) : BaseMigration
{
    public override Version Version => new(1, 2, 0);

    // Called BEFORE EF Core migrations - schema hasn't changed yet
    public override async Task PrepareMigrationAsync(IDictionary<string, object> cache)
    {
        var statuses = await db.Database
            .SqlQueryRaw<OldStatus>("SELECT Id, Status FROM Orders")
            .ToListAsync();

        cache["OrderStatuses"] = statuses;
    }

    // Called AFTER EF Core migrations - schema has changed
    public override async Task UpAsync()
    {
        if (Cache.TryGetValue("OrderStatuses", out var data))
        {
            var oldStatuses = (List<OldStatus>)data;

            foreach (var item in oldStatuses)
            {
                var newValue = item.Status switch
                {
                    0 => "pending",
                    1 => "confirmed",
                    2 => "shipped",
                    _ => "unknown"
                };

                await db.Database.ExecuteSqlAsync(
                    $"UPDATE Orders SET Status = {newValue} WHERE Id = {item.Id}");
            }
        }
    }
}
```

This keeps the data capture logic with the migration that needs it, rather than in a global engine hook.

## Advanced: Customizing EF Core Migration Execution

Override `RunEFCoreMigrationAsync` in your engine to customize how Entity Framework Core migrations are applied:

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

        if (!pendingMigrations.Any()) return;

        // Log each migration before applying
        foreach (var migration in pendingMigrations)
        {
            _logger.LogInformation("Pending EF Core migration: {Migration}", migration);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // Custom timeout (default is 15 minutes)
            dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            await dbContext.Database.MigrateAsync();
        });

        _logger.LogInformation("Applied {Count} EF Core migrations", pendingMigrations.Count());
    }
}
```

Common customizations include:
- **Custom command timeout**: Adjust for long-running migrations
- **Per-migration logging**: Log each migration name before/after application
- **Custom execution strategies**: Use different retry policies
- **Progress reporting**: Integrate with monitoring systems

## Advanced: Custom Version Storage

If you need custom storage (file, Redis, raw SQL, etc.), inherit from `BaseMigrationEngine` directly:

```csharp
public class MyMigrationEngine(MyDbContext db) : BaseMigrationEngine
{
    public override async Task<Version[]> GetAppliedVersionsAsync()
    {
        // Custom implementation
        return await db.AppliedMigrations
            .Select(m => new Version(m.Version))
            .ToArrayAsync();
    }

    public override async Task RegisterVersionAsync(Version version)
    {
        // Custom implementation
        db.AppliedMigrations.Add(new AppliedMigration { Version = version.ToString() });
        await db.SaveChangesAsync();
    }

    public override Task<bool> ShouldRunAsync()
    {
        // Custom condition
        return Task.FromResult(true);
    }
}
```

## Migration Discovery

Migrations are discovered automatically by scanning an assembly for classes inheriting from `BaseMigration`.

**Which assembly is scanned?**

| Registration Method | Assembly Scanned |
|---------------------|------------------|
| `AddApplicationMigrations<TEngine, TDbContext>()` | `TDbContext`'s assembly |
| `AddApplicationMigrations<TEngine>()` | `TEngine`'s assembly |

**Important:** When using built-in engines (`DefaultEfCoreMigrationEngine`, `DefaultSqlServerMigrationEngine`), always use the `<TEngine, TDbContext>` overload. This ensures migrations are discovered from your application's assembly (via the DbContext), not from the NuGet package.

```csharp
// Correct - migrations discovered from MyDbContext's assembly
builder.Services.AddApplicationMigrations<DefaultSqlServerMigrationEngine, MyDbContext>();

// Wrong - would scan the NuGet package assembly (no migrations there!)
builder.Services.AddApplicationMigrations<DefaultSqlServerMigrationEngine>();
```

## Async Support

```csharp
// Synchronous (blocks until complete)
app.UseMigrations();

// Asynchronous
await app.UseMigrationsAsync();

// With options
await app.UseMigrationsAsync(opts =>
{
    opts.EnforceLatestMigration = env.IsDevelopment();
});
```

## Demo Project

A complete demo application is included in the repository to show all features in action:

```bash
cd AreaProg.AspNetCore.Migrations.Demo
dotnet run
```

Then open http://localhost:5254/swagger to explore the API.

The demo includes:
- **AppMigrationEngine**: Full engine implementation with SQLite storage
- **V1_0_0_InitialSetup**: Demonstrates `FirstTime` for seed data
- **V1_1_0_AddCategories**: Demonstrates idempotent upsert patterns
- **V1_2_0_AddProductMetrics**: Demonstrates `Cache` for data capture

See the [Demo README](AreaProg.AspNetCore.Migrations.Demo/README.md) for details.

## Target Frameworks

- .NET 6.0
- .NET 8.0
- .NET 9.0
- .NET 10.0

## Migration Engine Hierarchy

```
BaseMigrationEngine (abstract)
├── ShouldRunAsync()              → override for custom conditions
├── GetAppliedVersionsAsync()     → abstract, must implement
├── RegisterVersionAsync()        → abstract, must implement
├── RunEFCoreMigrationAsync()     → override to customize EF Core migration execution
│
└── EfCoreMigrationEngine (abstract)
    ├── Auto-implements GetAppliedVersionsAsync() and RegisterVersionAsync()
    ├── Uses AppliedMigration entity
    │
    └── SqlServerMigrationEngine (abstract)
        └── Adds sp_getapplock distributed locking
```

## FAQ

### Should I keep old application migrations?

**No.** Unlike EF Core migrations (which must be preserved to recreate the database schema), application migrations can be deleted once applied to all environments.

Why delete them:
- **Maintenance burden**: Schema changes break old migrations. If you remove a field that was initialized in an old migration, you'd have to update that migration - making its code illogical and confusing.
- **No replay needed**: Application migrations are typically one-time data operations. You don't need to replay them from scratch like schema migrations.
- **History is in Git**: If you ever need to reference old migration code, it's preserved in your version control history.

**Recommended workflow:**
1. Write and deploy a migration
2. Once confirmed applied in production, delete the migration class
3. Keep only migrations for versions not yet deployed everywhere

This keeps your codebase clean and avoids maintaining code that will never run again.

### What happens if a migration fails?

When a `DbContext` is configured, each migration runs inside a database transaction. If an exception occurs:
- The transaction is rolled back automatically
- The version is **not** registered
- The application startup fails with the exception

This ensures your database remains in a consistent state. Fix the migration code and restart the application.

### How do I rollback a migration?

There is no automatic `DownAsync()` method. This is by design - rollback logic is rarely the exact inverse of the upgrade logic, especially for data migrations.

To undo a migration:
1. Write a new migration with a higher version
2. Implement the rollback logic in its `UpAsync()` method

```csharp
public class Migration_1_1_0_Rollback : BaseMigration
{
    public override Version Version => new(1, 1, 1);

    public override async Task UpAsync()
    {
        // Undo what 1.1.0 did
        await _db.Database.ExecuteSqlAsync($"DELETE FROM Settings WHERE Key = 'NewFeature'");
    }
}
```

### Can I use this without Entity Framework Core?

**Yes.** The `DbContext` configuration is optional. Without it:
- EF Core migrations are skipped
- Migrations run without transactions (you manage your own if needed)
- You still get version tracking via your engine implementation

```csharp
builder.Services.AddApplicationMigrations<MyMigrationEngine>();
// No options.DbContext = ... needed
```

Your engine can store versions anywhere: a file, Redis, a custom table via raw SQL, etc.

### What happens if two servers start simultaneously?

With `SqlServerMigrationEngine`, the `sp_getapplock` mechanism ensures only one instance acquires the lock. Other instances will skip migrations (with default timeout of 0) or wait (if you set a positive `LockTimeoutMs`).

For other databases, use the configuration-based approach to designate a single instance to run migrations.

## License

MIT
