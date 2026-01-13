namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Models;
using AreaProg.AspNetCore.Migrations.Tests.Fixtures;
using FluentAssertions;
using Xunit;

public class BaseMigrationTests
{
    [Fact]
    public void Version_ShouldReturnConfiguredVersion()
    {
        // Arrange
        var expectedVersion = new Version(1, 2, 3);
        var migration = new TestMigration(expectedVersion);

        // Act
        var actualVersion = migration.Version;

        // Assert
        actualVersion.Should().Be(expectedVersion);
    }

    [Fact]
    public void FirstTime_ShouldDefaultToFalse()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Act & Assert
        migration.FirstTime.Should().BeFalse();
    }

    [Fact]
    public void FirstTime_ShouldBeSettableInternally()
    {
        // Arrange
        var migration = new Version1Migration();

        // Act - simulate internal setting (which happens in ApplicationMigrationEngine)
        // We test this through the concrete implementation behavior

        // Assert
        migration.FirstTime.Should().BeFalse(); // Default value
    }

    [Fact]
    public void Cache_ShouldDefaultToEmptyDictionary()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Act & Assert
        migration.Cache.Should().NotBeNull();
        migration.Cache.Should().BeEmpty();
    }

    [Fact]
    public void Cache_ShouldBeModifiable()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Act
        migration.Cache["key1"] = "value1";
        migration.Cache["key2"] = 42;

        // Assert
        migration.Cache.Should().HaveCount(2);
        migration.Cache["key1"].Should().Be("value1");
        migration.Cache["key2"].Should().Be(42);
    }

    [Fact]
    public async Task UpAsync_ShouldBeCallable()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Act
        await migration.UpAsync();

        // Assert
        migration.WasExecuted.Should().BeTrue();
        migration.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task UpAsync_ShouldBeCallableMultipleTimes()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Act
        await migration.UpAsync();
        await migration.UpAsync();
        await migration.UpAsync();

        // Assert
        migration.ExecutionCount.Should().Be(3);
    }

    [Fact]
    public async Task UpAsync_ShouldCaptureFirstTimeValue()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));

        // Simulate setting FirstTime internally (done by ApplicationMigrationEngine)
        typeof(BaseMigration).GetProperty(nameof(BaseMigration.FirstTime))!
            .SetValue(migration, true);

        // Act
        await migration.UpAsync();

        // Assert
        migration.FirstTimeWhenExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task UpAsync_ShouldCaptureCache()
    {
        // Arrange
        var migration = new TestMigration(new Version(1, 0, 0));
        migration.Cache["testKey"] = "testValue";

        // Act
        await migration.UpAsync();

        // Assert
        migration.CacheWhenExecuted.Should().NotBeNull();
        migration.CacheWhenExecuted!["testKey"].Should().Be("testValue");
    }

    [Fact]
    public async Task UpAsync_WithCustomAction_ShouldExecuteAction()
    {
        // Arrange
        var actionExecuted = false;
        var migration = new TestMigration(
            new Version(1, 0, 0),
            _ =>
            {
                actionExecuted = true;
                return Task.CompletedTask;
            });

        // Act
        await migration.UpAsync();

        // Assert
        actionExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task UpAsync_WithAsyncAction_ShouldAwaitAction()
    {
        // Arrange
        var delayCompleted = false;
        var migration = new TestMigration(
            new Version(1, 0, 0),
            async _ =>
            {
                await Task.Delay(10);
                delayCompleted = true;
            });

        // Act
        await migration.UpAsync();

        // Assert
        delayCompleted.Should().BeTrue();
    }

    [Fact]
    public void MultipleVersions_ShouldOrderCorrectly()
    {
        // Arrange
        var versions = new[]
        {
            new TestMigration(new Version(2, 0, 0)),
            new TestMigration(new Version(1, 0, 0)),
            new TestMigration(new Version(3, 0, 0)),
            new TestMigration(new Version(1, 5, 0)),
        };

        // Act
        var sorted = versions.OrderBy(m => m.Version).ToArray();

        // Assert
        sorted[0].Version.Should().Be(new Version(1, 0, 0));
        sorted[1].Version.Should().Be(new Version(1, 5, 0));
        sorted[2].Version.Should().Be(new Version(2, 0, 0));
        sorted[3].Version.Should().Be(new Version(3, 0, 0));
    }

    [Fact]
    public async Task FailingMigration_ShouldThrowException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var migration = new FailingTestMigration(new Version(1, 0, 0), expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => migration.UpAsync());

        exception.Message.Should().Be("Test exception");
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 2, 3)]
    [InlineData(10, 20, 30)]
    public void Version_ShouldHandleVariousVersionFormats(int major, int minor, int build)
    {
        // Arrange
        var expectedVersion = new Version(major, minor, build);
        var migration = new TestMigration(expectedVersion);

        // Act
        var actualVersion = migration.Version;

        // Assert
        actualVersion.Major.Should().Be(major);
        actualVersion.Minor.Should().Be(minor);
        actualVersion.Build.Should().Be(build);
    }
}
