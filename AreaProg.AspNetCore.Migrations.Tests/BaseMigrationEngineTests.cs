namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Tests.Fixtures;
using FluentAssertions;
using Xunit;

public class BaseMigrationEngineTests
{
    [Fact]
    public async Task ShouldRunAsync_ShouldDefaultToTrue()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        var result = await engine.ShouldRunAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldRunAsync_WhenConfiguredFalse_ShouldReturnFalse()
    {
        // Arrange
        var engine = new TestMigrationEngine(shouldRun: false);

        // Act
        var result = await engine.ShouldRunAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DisabledMigrationEngine_ShouldRunAsync_ShouldReturnFalse()
    {
        // Arrange
        var engine = new DisabledMigrationEngine();

        // Act
        var result = await engine.ShouldRunAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAppliedVersionsAsync_WithNoVersions_ShouldReturnEmptyArray()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        var versions = await engine.GetAppliedVersionsAsync();

        // Assert
        versions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppliedVersionsAsync_WithVersions_ShouldReturnAllVersions()
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
        var versions = await engine.GetAppliedVersionsAsync();

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
        var appliedVersions = await engine.GetAppliedVersionsAsync();

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
    public async Task RunBeforeDatabaseMigrationAsync_ShouldBeCallable()
    {
        // Arrange
        var engine = new TestMigrationEngine();

        // Act
        await engine.RunBeforeDatabaseMigrationAsync();

        // Assert
        engine.RunBeforeDatabaseMigrationAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunBeforeDatabaseMigrationAsync_WithCallback_ShouldExecuteCallback()
    {
        // Arrange
        var callbackExecuted = false;
        var engine = new TestMigrationEngine
        {
            OnRunBeforeDatabaseMigrationAsync = () =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            }
        };

        // Act
        await engine.RunBeforeDatabaseMigrationAsync();

        // Assert
        callbackExecuted.Should().BeTrue();
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
    public void FailingMigrationEngine_GetAppliedVersionsAsync_ShouldThrow()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var engine = new FailingMigrationEngine(expectedException);

        // Act & Assert
        var action = async () => await engine.GetAppliedVersionsAsync();
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
            OnRunBeforeDatabaseMigrationAsync = () =>
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
        await engine.RunBeforeDatabaseMigrationAsync();
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
        var versions = await engine.GetAppliedVersionsAsync();
        versions.Should().Contain(version);
    }
}
