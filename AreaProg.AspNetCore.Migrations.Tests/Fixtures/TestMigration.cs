namespace AreaProg.AspNetCore.Migrations.Tests.Fixtures;

using AreaProg.AspNetCore.Migrations.Models;
using System;
using System.Threading.Tasks;

/// <summary>
/// Test migration implementation for unit testing.
/// Has a parameterless constructor so it can be discovered by the migration engine.
/// </summary>
public class TestMigration : BaseMigration
{
    private readonly Version _version;
    private readonly Func<BaseMigration, Task>? _upAction;

    public bool WasExecuted { get; private set; }
    public int ExecutionCount { get; private set; }
    public bool FirstTimeWhenExecuted { get; private set; }
    public IDictionary<string, object>? CacheWhenExecuted { get; private set; }

    /// <summary>
    /// Parameterless constructor for DI resolution.
    /// Creates a migration with version 0.0.1 (will be skipped if other migrations exist).
    /// </summary>
    public TestMigration() : this(new Version(0, 0, 1), null)
    {
    }

    public TestMigration(Version version, Func<BaseMigration, Task>? upAction = null)
    {
        _version = version;
        _upAction = upAction;
    }

    public override Version Version => _version;

    public override async Task UpAsync()
    {
        WasExecuted = true;
        ExecutionCount++;
        FirstTimeWhenExecuted = FirstTime;
        CacheWhenExecuted = new Dictionary<string, object>(Cache);

        if (_upAction != null)
        {
            await _upAction(this);
        }
    }
}

/// <summary>
/// Test migration that throws an exception.
/// Has a parameterless constructor so it can be discovered by the migration engine
/// (but won't cause issues since it's at version 0.0.0 and will be skipped).
/// </summary>
public class FailingTestMigration : BaseMigration
{
    private readonly Version _version;
    private readonly Exception _exception;

    /// <summary>
    /// Parameterless constructor for DI resolution.
    /// Creates a migration at version 0.0.0 that does nothing.
    /// </summary>
    public FailingTestMigration() : this(new Version(0, 0, 0), new NotImplementedException("Default constructor - should not be executed"))
    {
    }

    public FailingTestMigration(Version version, Exception exception)
    {
        _version = version;
        _exception = exception;
    }

    public override Version Version => _version;

    public override Task UpAsync()
    {
        // Only throw if this is not the default constructor version
        if (_version > new Version(0, 0, 0))
        {
            throw _exception;
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple concrete migration for version 1.0.0.
/// This will be auto-discovered by the migration engine.
/// </summary>
public class Version1Migration : BaseMigration
{
    public static bool WasExecuted { get; private set; }
    public static bool FirstTimeWhenExecuted { get; private set; }
    public static int ExecutionCount { get; private set; }

    public static void Reset()
    {
        WasExecuted = false;
        FirstTimeWhenExecuted = false;
        ExecutionCount = 0;
    }

    public override Version Version => new(1, 0, 0);

    public override Task UpAsync()
    {
        WasExecuted = true;
        FirstTimeWhenExecuted = FirstTime;
        ExecutionCount++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple concrete migration for version 2.0.0.
/// This will be auto-discovered by the migration engine.
/// </summary>
public class Version2Migration : BaseMigration
{
    public static bool WasExecuted { get; private set; }
    public static bool FirstTimeWhenExecuted { get; private set; }
    public static int ExecutionCount { get; private set; }

    public static void Reset()
    {
        WasExecuted = false;
        FirstTimeWhenExecuted = false;
        ExecutionCount = 0;
    }

    public override Version Version => new(2, 0, 0);

    public override Task UpAsync()
    {
        WasExecuted = true;
        FirstTimeWhenExecuted = FirstTime;
        ExecutionCount++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Simple concrete migration for version 3.0.0.
/// This will be auto-discovered by the migration engine.
/// </summary>
public class Version3Migration : BaseMigration
{
    public static bool WasExecuted { get; private set; }
    public static bool FirstTimeWhenExecuted { get; private set; }
    public static int ExecutionCount { get; private set; }

    public static void Reset()
    {
        WasExecuted = false;
        FirstTimeWhenExecuted = false;
        ExecutionCount = 0;
    }

    public override Version Version => new(3, 0, 0);

    public override Task UpAsync()
    {
        WasExecuted = true;
        FirstTimeWhenExecuted = FirstTime;
        ExecutionCount++;
        return Task.CompletedTask;
    }
}
