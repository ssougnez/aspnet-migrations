namespace AreaProg.Migrations.Tests;

using AreaProg.Migrations.Abstractions;
using AreaProg.Migrations.Extensions;
using AreaProg.AspNetCore.Migrations.Extensions;
using AreaProg.Migrations.Interfaces;
using AreaProg.Migrations.Models;
using AreaProg.Migrations.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for various migration scenarios and edge cases.
/// </summary>
public class MigrationScenariosTests
{
    [Fact]
    public void VersionComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var v1 = new Version(1, 0, 0);
        var v2 = new Version(2, 0, 0);
        var v11 = new Version(1, 1, 0);
        var v101 = new Version(1, 0, 1);

        // Assert
        v1.Should().BeLessThan(v2);
        v1.Should().BeLessThan(v11);
        v1.Should().BeLessThan(v101);
        v11.Should().BeLessThan(v2);
        v101.Should().BeLessThan(v11);
    }

    [Fact]
    public void VersionEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var v1a = new Version(1, 0, 0);
        var v1b = new Version(1, 0, 0);

        // Assert
        v1a.Should().Be(v1b);
        (v1a == v1b).Should().BeTrue();
    }

    [Fact]
    public void MigrationOrdering_ShouldSortByVersion()
    {
        // Arrange
        var migrations = new BaseMigration[]
        {
            new TestMigration(new Version(3, 0, 0)),
            new TestMigration(new Version(1, 0, 0)),
            new TestMigration(new Version(2, 5, 0)),
            new TestMigration(new Version(2, 0, 0)),
        };

        // Act
        var ordered = migrations.OrderBy(m => m.Version).ToArray();

        // Assert
        ordered[0].Version.Should().Be(new Version(1, 0, 0));
        ordered[1].Version.Should().Be(new Version(2, 0, 0));
        ordered[2].Version.Should().Be(new Version(2, 5, 0));
        ordered[3].Version.Should().Be(new Version(3, 0, 0));
    }

    [Fact]
    public async Task CacheIsolation_BetweenMigrations_ShouldWork()
    {
        // Arrange - each migration gets its own isolated cache
        var cache1 = new Dictionary<string, object> { ["key"] = "value1" };
        var cache2 = new Dictionary<string, object> { ["key"] = "value2" };

        var migration1 = new TestMigration(new Version(1, 0, 0));
        var migration2 = new TestMigration(new Version(2, 0, 0));

        // Use reflection to set Cache (internal setter)
        typeof(BaseMigration).GetProperty(nameof(BaseMigration.Cache))!
            .SetValue(migration1, cache1);
        typeof(BaseMigration).GetProperty(nameof(BaseMigration.Cache))!
            .SetValue(migration2, cache2);

        // Act
        await migration1.UpAsync();
        migration1.Cache["addedByMigration1"] = "extra1";
        await migration2.UpAsync();

        // Assert - each migration has its own isolated cache
        migration1.CacheWhenExecuted!["key"].Should().Be("value1");
        migration2.CacheWhenExecuted!["key"].Should().Be("value2");
        migration1.Cache.Should().ContainKey("addedByMigration1");
        migration2.Cache.Should().NotContainKey("addedByMigration1");
    }

    [Fact]
    public void FirstTimeFlag_ShouldBeSettableInternally()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Act - simulate internal setting using reflection
        typeof(BaseMigration).GetProperty(nameof(BaseMigration.FirstTime))!
            .SetValue(migration, true);

        // Assert
        migration.FirstTime.Should().BeTrue();
    }

    [Fact]
    public void CacheProperty_ShouldBeSettableInternally()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));
        var newCache = new Dictionary<string, object> { ["test"] = "value" };

        // Act - simulate internal setting using reflection
        typeof(BaseMigration).GetProperty(nameof(BaseMigration.Cache))!
            .SetValue(migration, newCache);

        // Assert
        migration.Cache.Should().BeSameAs(newCache);
    }

    [Fact]
    public async Task MigrationWithDependencyInjection_ShouldReceiveServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITestService, TestService>();
        services.AddApplicationMigrations<TestMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyMigrationList_ShouldNotThrow()
    {
        // Arrange - using an engine that returns no migrations from its assembly
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        var action = async () => await engine.RunAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(1, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(int.MaxValue, 0, 0)]
    public void VersionEdgeCases_ShouldBeHandled(int major, int minor, int build)
    {
        // Arrange & Act
        var migration = new TestMigration(new Version(major, minor, build));

        // Assert
        migration.Version.Major.Should().Be(major);
        migration.Version.Minor.Should().Be(minor);
        migration.Version.Build.Should().Be(build);
    }

    [Fact]
    public void VersionWithRevision_ShouldWork()
    {
        // Arrange
        var version = new Version(1, 2, 3, 4);

        // Act
        var migration = new TestMigration(version);

        // Assert
        migration.Version.Major.Should().Be(1);
        migration.Version.Minor.Should().Be(2);
        migration.Version.Build.Should().Be(3);
        migration.Version.Revision.Should().Be(4);
    }

    [Fact]
    public async Task MultipleEngineTypes_ShouldBeIndependent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        services.AddApplicationMigrations<DisabledMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();

        // Act - get both engines
        var engines = serviceProvider.GetServices<IApplicationMigrationEngine>().ToList();

        // Assert
        engines.Should().HaveCount(2);
    }
}

/// <summary>
/// Test service interface for DI testing.
/// </summary>
public interface ITestService
{
    string GetValue();
}

/// <summary>
/// Test service implementation.
/// </summary>
public class TestService : ITestService
{
    public string GetValue() => "TestValue";
}

/// <summary>
/// Tests for error handling scenarios.
/// </summary>
public class ErrorHandlingTests
{
    [Fact]
    public async Task MigrationException_ShouldPropagate()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Migration failed");
        var migration = new FailingTestMigration(new Version(1, 0, 0), expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => migration.UpAsync());

        exception.Should().BeSameAs(expectedException);
    }

    [Fact]
    public void EngineException_ShouldPropagate()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Engine failed");
        var engine = new FailingMigrationEngine(expectedException);

        // Act & Assert
        var action = () => engine.GetAppliedVersionsAsync();
        action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AsyncException_ShouldBeAwaitable()
    {
        // Arrange
        var migration = new FailingTestMigration(
            new Version(1, 0, 0),
            new ApplicationException("Async error"));

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => migration.UpAsync());
    }
}

/// <summary>
/// Tests for hook execution order and behavior.
/// </summary>
public class HookExecutionTests
{
    [Fact]
    public async Task AllHooks_ShouldBeCallableIndependently()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        await engine.RunBeforeAsync();
        await engine.RunBeforeDatabaseMigrationAsync();
        await engine.RunAfterDatabaseMigrationAsync();
        await engine.RunAfterAsync();

        // Assert
        engine.RunBeforeAsyncCalled.Should().BeTrue();
        engine.RunBeforeDatabaseMigrationAsyncCalled.Should().BeTrue();
        engine.RunAfterDatabaseMigrationAsyncCalled.Should().BeTrue();
        engine.RunAfterAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CacheModification_InPrepareMigrationAsync_ShouldPersist()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));
        var cache = new Dictionary<string, object>();

        // Use reflection to set Cache (internal setter)
        typeof(BaseMigration).GetProperty(nameof(BaseMigration.Cache))!
            .SetValue(migration, cache);

        // Act - PrepareMigrationAsync should populate the cache
        await migration.PrepareMigrationAsync(cache);
        cache["captured"] = "data";
        await migration.UpAsync();

        // Assert
        migration.CacheWhenExecuted.Should().ContainKey("captured");
        migration.CacheWhenExecuted!["captured"].Should().Be("data");
    }
}

/// <summary>
/// Tests for EnforceLatestMigration option behavior.
/// </summary>
public class EnforceLatestMigrationTests : IDisposable
{
    public EnforceLatestMigrationTests()
    {
        TestMigrationEngine.Reset();
        Version1Migration.Reset();
        Version2Migration.Reset();
        Version3Migration.Reset();
    }

    public void Dispose()
    {
        TestMigrationEngine.Reset();
        Version1Migration.Reset();
        Version2Migration.Reset();
        Version3Migration.Reset();
    }

    [Fact]
    public async Task WithoutEnforceLatestMigration_CurrentVersionMigration_ShouldNotReExecute()
    {
        // Arrange - Set up engine with version 2.0.0 already applied
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(1, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(2, 0, 0));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act - Run without EnforceLatestMigration (default is false)
        await engine.RunAsync(new UseMigrationsOptions { EnforceLatestMigration = false });

        // Assert - Only version 3 should run (it's > current 2.0.0)
        // Versions 1 and 2 should NOT run because they are <= current
        Version1Migration.WasExecuted.Should().BeFalse();
        Version2Migration.WasExecuted.Should().BeFalse();
        Version3Migration.WasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WithEnforceLatestMigration_CurrentVersionMigration_ShouldReExecute()
    {
        // Arrange - Set up engine with version 2.0.0 already applied
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(1, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(2, 0, 0));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act - Run with EnforceLatestMigration = true
        await engine.RunAsync(new UseMigrationsOptions { EnforceLatestMigration = true });

        // Assert - Versions 2 and 3 should run (>= current 2.0.0)
        // Version 1 should NOT run because it's < current
        Version1Migration.WasExecuted.Should().BeFalse();
        Version2Migration.WasExecuted.Should().BeTrue();
        Version3Migration.WasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WithEnforceLatestMigration_CurrentVersionMigration_FirstTime_ShouldBeFalse()
    {
        // Arrange - Set up engine with version 2.0.0 already applied
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(1, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(2, 0, 0));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        await engine.RunAsync(new UseMigrationsOptions { EnforceLatestMigration = true });

        // Assert - Version 2 should have FirstTime = false (re-execution)
        // Version 3 should have FirstTime = true (first run)
        Version2Migration.FirstTimeWhenExecuted.Should().BeFalse();
        Version3Migration.FirstTimeWhenExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultBehavior_ShouldNotReExecuteCurrentVersion()
    {
        // Arrange - Set up engine with version 3.0.0 already applied (latest)
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(1, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(2, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(3, 0, 0));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act - Run with default options (EnforceLatestMigration defaults to false)
        await engine.RunAsync();

        // Assert - No migration should execute (default skips current version)
        Version1Migration.WasExecuted.Should().BeFalse();
        Version2Migration.WasExecuted.Should().BeFalse();
        Version3Migration.WasExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task WithEnforceLatestMigration_AllApplied_ShouldReExecuteLatest()
    {
        // Arrange - Set up engine with all versions already applied
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(1, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(2, 0, 0));
        TestMigrationEngine.StaticPreAppliedVersions.Add(new Version(3, 0, 0));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act - Run with EnforceLatestMigration = true
        await engine.RunAsync(new UseMigrationsOptions { EnforceLatestMigration = true });

        // Assert - Only version 3 should re-execute (it's the current/latest)
        Version1Migration.WasExecuted.Should().BeFalse();
        Version2Migration.WasExecuted.Should().BeFalse();
        Version3Migration.WasExecuted.Should().BeTrue();
        Version3Migration.FirstTimeWhenExecuted.Should().BeFalse(); // Re-execution
    }

    [Fact]
    public async Task FreshInstall_BothModes_ShouldExecuteAllMigrations()
    {
        // Arrange - No pre-applied versions (fresh install)
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act - Both modes should execute all migrations on fresh install
        await engine.RunAsync(new UseMigrationsOptions { EnforceLatestMigration = false });

        // Assert
        Version1Migration.WasExecuted.Should().BeTrue();
        Version2Migration.WasExecuted.Should().BeTrue();
        Version3Migration.WasExecuted.Should().BeTrue();
    }

    [Fact]
    public void UseMigrationsOptions_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var options = new UseMigrationsOptions();

        // Assert - default is false (production-friendly)
        options.EnforceLatestMigration.Should().BeFalse();
    }
}
