namespace AreaProg.Migrations.Tests.Fixtures;

using AreaProg.Migrations.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Test migration engine implementation for unit testing.
/// Uses static properties to track calls since ActivatorUtilities creates new instances.
/// </summary>
public class TestMigrationEngine : BaseMigrationEngine
{
    private readonly List<Version> _appliedVersions = new();
    private readonly List<Version> _registeredVersions = new();
    private readonly bool _shouldRun;

    // Static tracking properties (reset between tests)
    public static bool StaticRunBeforeAsyncCalled { get; private set; }
    public static bool StaticRunAfterAsyncCalled { get; private set; }
    public static bool StaticRunBeforeDatabaseMigrationAsyncCalled { get; private set; }
    public static bool StaticRunAfterDatabaseMigrationAsyncCalled { get; private set; }
    public static List<Version> StaticRegisteredVersions { get; } = new();
    public static int StaticInstanceCount { get; private set; }
    public static List<string> StaticCallOrder { get; } = new();
    public static List<Version> StaticPreAppliedVersions { get; } = new();

    // Instance tracking properties
    public bool RunBeforeAsyncCalled { get; private set; }
    public bool RunAfterAsyncCalled { get; private set; }
    public bool RunBeforeDatabaseMigrationAsyncCalled { get; private set; }
    public bool RunAfterDatabaseMigrationAsyncCalled { get; private set; }
    public IReadOnlyList<Version> RegisteredVersions => _registeredVersions;

    // Instance callbacks (for direct instantiation tests)
    public Func<Task>? OnRunBeforeAsync { get; set; }
    public Func<Task>? OnRunAfterAsync { get; set; }
    public Func<Task>? OnRunBeforeDatabaseMigrationAsync { get; set; }
    public Func<Task>? OnRunAfterDatabaseMigrationAsync { get; set; }

    public static void Reset()
    {
        StaticRunBeforeAsyncCalled = false;
        StaticRunAfterAsyncCalled = false;
        StaticRunBeforeDatabaseMigrationAsyncCalled = false;
        StaticRunAfterDatabaseMigrationAsyncCalled = false;
        StaticRegisteredVersions.Clear();
        StaticInstanceCount = 0;
        StaticCallOrder.Clear();
        StaticPreAppliedVersions.Clear();
    }

    public TestMigrationEngine() : this(true)
    {
        StaticInstanceCount++;
    }

    public TestMigrationEngine(bool shouldRun)
    {
        _shouldRun = shouldRun;
        StaticInstanceCount++;
        // Copy any pre-applied versions from static configuration
        _appliedVersions.AddRange(StaticPreAppliedVersions);
    }

    public TestMigrationEngine(IEnumerable<Version> appliedVersions) : this(true)
    {
        _appliedVersions.AddRange(appliedVersions);
    }

    public override Task<bool> ShouldRunAsync() => Task.FromResult(_shouldRun);

    public void AddAppliedVersion(Version version)
    {
        _appliedVersions.Add(version);
    }

    public override Task<Version[]> GetAppliedVersionsAsync()
    {
        return Task.FromResult(_appliedVersions.ToArray());
    }

    public override Task RegisterVersionAsync(Version version)
    {
        _registeredVersions.Add(version);
        _appliedVersions.Add(version);
        StaticRegisteredVersions.Add(version);
        return Task.CompletedTask;
    }

    public override async Task RunBeforeAsync()
    {
        RunBeforeAsyncCalled = true;
        StaticRunBeforeAsyncCalled = true;
        StaticCallOrder.Add("RunBefore");

        if (OnRunBeforeAsync != null)
        {
            await OnRunBeforeAsync();
        }
    }

    public override async Task RunAfterAsync()
    {
        RunAfterAsyncCalled = true;
        StaticRunAfterAsyncCalled = true;
        StaticCallOrder.Add("RunAfter");

        if (OnRunAfterAsync != null)
        {
            await OnRunAfterAsync();
        }
    }

    public override async Task RunBeforeDatabaseMigrationAsync()
    {
        RunBeforeDatabaseMigrationAsyncCalled = true;
        StaticRunBeforeDatabaseMigrationAsyncCalled = true;
        StaticCallOrder.Add("RunBeforeDatabaseMigration");

        if (OnRunBeforeDatabaseMigrationAsync != null)
        {
            await OnRunBeforeDatabaseMigrationAsync();
        }
    }

    public override async Task RunAfterDatabaseMigrationAsync()
    {
        RunAfterDatabaseMigrationAsyncCalled = true;
        StaticRunAfterDatabaseMigrationAsyncCalled = true;
        StaticCallOrder.Add("RunAfterDatabaseMigration");

        if (OnRunAfterDatabaseMigrationAsync != null)
        {
            await OnRunAfterDatabaseMigrationAsync();
        }
    }
}

/// <summary>
/// Migration engine that always throws an exception when getting applied versions.
/// </summary>
public class FailingMigrationEngine : BaseMigrationEngine
{
    private readonly Exception _exception;

    public FailingMigrationEngine() : this(new InvalidOperationException("Default failure"))
    {
    }

    public FailingMigrationEngine(Exception exception)
    {
        _exception = exception;
    }

    public override Task<Version[]> GetAppliedVersionsAsync()
    {
        throw _exception;
    }

    public override Task RegisterVersionAsync(Version version)
    {
        throw _exception;
    }
}

/// <summary>
/// Migration engine that is configured not to run.
/// </summary>
public class DisabledMigrationEngine : BaseMigrationEngine
{
    public override Task<bool> ShouldRunAsync() => Task.FromResult(false);

    public override Task<Version[]> GetAppliedVersionsAsync()
    {
        return Task.FromResult(Array.Empty<Version>());
    }

    public override Task RegisterVersionAsync(Version version)
    {
        return Task.CompletedTask;
    }
}
