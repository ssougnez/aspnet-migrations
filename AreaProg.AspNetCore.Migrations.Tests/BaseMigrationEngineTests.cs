namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Tests.Fixtures;
using FluentAssertions;
using Xunit;

public class BaseMigrationEngineTests
{
    [Fact]
    public void ShouldRun_ShouldDefaultToTrue()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act & Assert
        engine.ShouldRun.Should().BeTrue();
    }

    [Fact]
    public void ShouldRun_WhenConfiguredFalse_ShouldReturnFalse()
    {
        // Arrange
        var engine = new TestMigrationEngine(shouldRun: false);

        // Act & Assert
        engine.ShouldRun.Should().BeFalse();
    }

    [Fact]
    public void DisabledMigrationEngine_ShouldRun_ShouldReturnFalse()
    {
        // Arrange
        var engine = new DisabledMigrationEngine();

        // Act & Assert
        engine.ShouldRun.Should().BeFalse();
    }

    [Fact]
    public async Task GetAppliedVersionAsync_WithNoVersions_ShouldReturnEmptyArray()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        var versions = await engine.GetAppliedVersionAsync();

        // Assert
        versions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppliedVersionAsync_WithVersions_ShouldReturnAllVersions()
    {
        // Arrange
        var expectedVersions = new[]
        {
            new Version(1, 0, 0),
            new Version(2, 0, 0),
            new Version(3, 0, 0)
        };
        var engine = new TestMigrationEngine(expectedVersions);

        // Act
        var versions = await engine.GetAppliedVersionAsync();

        // Assert
        versions.Should().BeEquivalentTo(expectedVersions);
    }

    [Fact]
    public async Task RegisterVersionAsync_ShouldAddVersion()
    {
        // Arrange
        var engine = new TestMigrationEngine();
        var version = new Version(1, 0, 0);

        // Act
        await engine.RegisterVersionAsync(version);

        // Assert
        engine.RegisteredVersions.Should().Contain(version);
    }

    [Fact]
    public async Task RegisterVersionAsync_CalledMultipleTimes_ShouldAddAllVersions()
    {
        // Arrange
        var engine = new TestMigrationEngine();
        var versions = new[]
        {
            new Version(1, 0, 0),
            new Version(2, 0, 0),
            new Version(3, 0, 0)
        };

        // Act
        foreach (var version in versions)
        {
            await engine.RegisterVersionAsync(version);
        }

        // Assert
        engine.RegisteredVersions.Should().BeEquivalentTo(versions);
    }

    [Fact]
    public async Task RegisterVersionAsync_ShouldAlsoAddToAppliedVersions()
    {
        // Arrange
        var engine = new TestMigrationEngine();
        var version = new Version(1, 0, 0);

        // Act
        await engine.RegisterVersionAsync(version);
        var appliedVersions = await engine.GetAppliedVersionAsync();

        // Assert
        appliedVersions.Should().Contain(version);
    }

    [Fact]
    public async Task RunBeforeAsync_ShouldBeCallable()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        await engine.RunBeforeAsync();

        // Assert
        engine.RunBeforeAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunBeforeAsync_WithCallback_ShouldExecuteCallback()
    {
        // Arrange
        var callbackExecuted = false;
        var engine = new TestMigrationEngine
        {
            OnRunBeforeAsync = () =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            }
        };

        // Act
        await engine.RunBeforeAsync();

        // Assert
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task RunAfterAsync_ShouldBeCallable()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        await engine.RunAfterAsync();

        // Assert
        engine.RunAfterAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAfterAsync_WithCallback_ShouldExecuteCallback()
    {
        // Arrange
        var callbackExecuted = false;
        var engine = new TestMigrationEngine
        {
            OnRunAfterAsync = () =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            }
        };

        // Act
        await engine.RunAfterAsync();

        // Assert
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task RunBeforeDatabaseMigrationAsync_ShouldReceiveCache()
    {
        // Arrange
        var engine = new TestMigrationEngine();
        var cache = new Dictionary<string, object>
        {
            ["key1"] = "value1"
        };

        // Act
        await engine.RunBeforeDatabaseMigrationAsync(cache);

        // Assert
        engine.RunBeforeDatabaseMigrationAsyncCalled.Should().BeTrue();
        engine.CachePassedToRunBeforeDatabaseMigration.Should().BeSameAs(cache);
    }

    [Fact]
    public async Task RunBeforeDatabaseMigrationAsync_WithCallback_ShouldPassCache()
    {
        // Arrange
        IDictionary<string, object>? receivedCache = null;
        var engine = new TestMigrationEngine
        {
            OnRunBeforeDatabaseMigrationAsync = cache =>
            {
                receivedCache = cache;
                cache["addedByCallback"] = "test";
                return Task.CompletedTask;
            }
        };
        var inputCache = new Dictionary<string, object>();

        // Act
        await engine.RunBeforeDatabaseMigrationAsync(inputCache);

        // Assert
        receivedCache.Should().BeSameAs(inputCache);
        inputCache.Should().ContainKey("addedByCallback");
    }

    [Fact]
    public async Task RunAfterDatabaseMigrationAsync_ShouldBeCallable()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        await engine.RunAfterDatabaseMigrationAsync();

        // Assert
        engine.RunAfterDatabaseMigrationAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAfterDatabaseMigrationAsync_WithCallback_ShouldExecuteCallback()
    {
        // Arrange
        var callbackExecuted = false;
        var engine = new TestMigrationEngine
        {
            OnRunAfterDatabaseMigrationAsync = () =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            }
        };

        // Act
        await engine.RunAfterDatabaseMigrationAsync();

        // Assert
        callbackExecuted.Should().BeTrue();
    }

    [Fact]
    public void FailingMigrationEngine_GetAppliedVersionAsync_ShouldThrow()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var engine = new FailingMigrationEngine(expectedException);

        // Act & Assert
        var action = async () => await engine.GetAppliedVersionAsync();
        action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test error");
    }

    [Fact]
    public void FailingMigrationEngine_RegisterVersionAsync_ShouldThrow()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var engine = new FailingMigrationEngine(expectedException);

        // Act & Assert
        var action = async () => await engine.RegisterVersionAsync(new Version(1, 0, 0));
        action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test error");
    }

    [Fact]
    public async Task HooksOrder_ShouldBeTracked()
    {
        // Arrange
        var callOrder = new List<string>();
        var engine = new TestMigrationEngine
        {
            OnRunBeforeAsync = () =>
            {
                callOrder.Add("RunBefore");
                return Task.CompletedTask;
            },
            OnRunBeforeDatabaseMigrationAsync = _ =>
            {
                callOrder.Add("RunBeforeDatabaseMigration");
                return Task.CompletedTask;
            },
            OnRunAfterDatabaseMigrationAsync = () =>
            {
                callOrder.Add("RunAfterDatabaseMigration");
                return Task.CompletedTask;
            },
            OnRunAfterAsync = () =>
            {
                callOrder.Add("RunAfter");
                return Task.CompletedTask;
            }
        };

        // Act - simulate the expected hook order
        await engine.RunBeforeAsync();
        await engine.RunBeforeDatabaseMigrationAsync(new Dictionary<string, object>());
        await engine.RunAfterDatabaseMigrationAsync();
        await engine.RunAfterAsync();

        // Assert
        callOrder.Should().Equal("RunBefore", "RunBeforeDatabaseMigration", "RunAfterDatabaseMigration", "RunAfter");
    }

    [Fact]
    public async Task AddAppliedVersion_ShouldAddToAppliedVersionsList()
    {
        // Arrange
        var engine = new TestMigrationEngine();
        var version = new Version(1, 0, 0);

        // Act
        engine.AddAppliedVersion(version);

        // Assert
        var versions = await engine.GetAppliedVersionAsync();
        versions.Should().Contain(version);
    }
}
