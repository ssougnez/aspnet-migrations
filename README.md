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

## Quick Start

### 1. Create a Table to Track Versions

Add a simple entity and DbSet to your existing DbContext:

```csharp
public class AppliedMigration
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
}

public class MyDbContext : DbContext
{
    public DbSet<AppliedMigration> AppliedMigrations { get; set; }

    // ... your other DbSets
}
```

### 2. Create a Migration Engine

The engine tracks which versions have been applied:

```csharp
public class MyMigrationEngine : BaseMigrationEngine
{
    private readonly MyDbContext _db;

    public MyMigrationEngine(MyDbContext db)
    {
        _db = db;
    }

    public override async Task<Version[]> GetAppliedVersionAsync()
    {
        return await _db.AppliedMigrations
            .Select(m => new Version(m.Version))
            .ToArrayAsync();
    }

    public override async Task RegisterVersionAsync(Version version)
    {
        _db.AppliedMigrations.Add(new AppliedMigration { Version = version.ToString() });
        await _db.SaveChangesAsync();
    }
}
```

### 3. Create a Migration

```csharp
public class Migration_1_0_0 : BaseMigration
{
    private readonly MyDbContext _db;

    public Migration_1_0_0(MyDbContext db)
    {
        _db = db;
    }

    public override Version Version => new(1, 0, 0);

    public override async Task UpAsync()
    {
        // Your migration code here
        await _db.Users.AddAsync(new User { Name = "Admin", IsAdmin = true });
        await _db.SaveChangesAsync();
    }
}
```

### 4. Register and Run

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(...);

builder.Services.AddApplicationMigrations<MyMigrationEngine>(options =>
{
    options.DbContext = typeof(MyDbContext);
});

var app = builder.Build();

app.UseMigrations();

app.Run();
```

That's it! At startup:
1. EF Core database migrations run automatically
2. Each application migration runs in a transaction
3. Versions are tracked in your database

## Using Dependency Injection

Migrations support constructor injection:

```csharp
public class Migration_1_1_0 : BaseMigration
{
    private readonly MyDbContext _db;
    private readonly IEmailService _email;

    public Migration_1_1_0(MyDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public override Version Version => new(1, 1, 0);

    public override async Task UpAsync()
    {
        var admins = await _db.Users.Where(u => u.IsAdmin).ToListAsync();

        foreach (var admin in admins)
        {
            await _email.SendAsync(admin.Email, "System upgraded to 1.1.0");
        }
    }
}
```

## The `FirstTime` Property

### Why Re-execution?

The migration matching the current registered version is **re-executed on each application startup**. This is intentional to facilitate development workflows:

- You can iterate on a migration without manually rolling back the database version
- No need to delete version records or reset state between debugging sessions
- Test your migration logic repeatedly until it works correctly

### Handling Re-execution

To handle this behavior, you have two strategies:

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
public class MyMigrationEngine : BaseMigrationEngine
{
    // Called before any migrations
    public override Task RunBeforeAsync() { ... }

    // Called before EF Core migrations (only if migrations are pending)
    public override Task RunBeforeDatabaseMigrationAsync(IDictionary<string, object> cache) { ... }

    // Called after EF Core migrations
    public override Task RunAfterDatabaseMigrationAsync() { ... }

    // Called after all application migrations
    public override Task RunAfterAsync() { ... }
}
```

### Execution Order

```
1. RunBeforeAsync()
2. RunBeforeDatabaseMigrationAsync(cache)  [if EF Core migrations pending]
3. EF Core MigrateAsync()
4. RunAfterDatabaseMigrationAsync()
5. Application migrations (UpAsync for each)
6. RunAfterAsync()
```

## Advanced: Data Transformation During Schema Changes

When changing column types (e.g., `int` enum to `string`), you need to capture data before the schema change. Use the `Cache`:

```csharp
// In your engine
public override async Task RunBeforeDatabaseMigrationAsync(IDictionary<string, object> cache)
{
    // Capture data BEFORE schema change
    var statuses = await _db.Database
        .SqlQueryRaw<OldStatus>("SELECT Id, Status FROM Orders")
        .ToListAsync();

    cache["OrderStatuses"] = statuses;
}

// In your migration
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

            await _db.Database.ExecuteSqlAsync(
                $"UPDATE Orders SET Status = {newValue} WHERE Id = {item.Id}");
        }
    }
}
```

## Conditional Execution

Use `ShouldRun` to skip migrations based on runtime conditions.

### Skip in Test Environment

```csharp
public class MyMigrationEngine : BaseMigrationEngine
{
    private readonly IHostEnvironment _env;

    public MyMigrationEngine(IHostEnvironment env)
    {
        _env = env;
    }

    public override bool ShouldRun => !_env.IsEnvironment("Test");
}
```

### Multi-Server Deployments

When deploying to multiple servers (load-balanced, Kubernetes replicas, etc.), only **one instance** should run migrations to avoid conflicts. Designate a "master" server via configuration:

```json
// appsettings.json (on the master server only)
{
  "Migrations": {
    "IsMaster": true
  }
}
```

```csharp
public class MigrationSettings
{
    public bool IsMaster { get; set; }
}

public class MyMigrationEngine : BaseMigrationEngine
{
    private readonly MigrationSettings _settings;

    public MyMigrationEngine(IOptions<MigrationSettings> settings)
    {
        _settings = settings.Value;
    }

    public override bool ShouldRun => _settings.IsMaster;
}
```

```csharp
// Program.cs
builder.Services.Configure<MigrationSettings>(
    builder.Configuration.GetSection("Migrations"));
```

This ensures migrations run only on the designated master instance, while other instances skip them and start normally.

## Async Support

```csharp
// Synchronous (blocks until complete)
app.UseMigrations();

// Asynchronous
await app.UseMigrationsAsync();
```

## Target Frameworks

- .NET 6.0
- .NET 8.0
- .NET 9.0
- .NET 10.0

## License

MIT
