namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Tests for the IApplicationMigrationEngine interface contract.
/// </summary>
public class IApplicationMigrationEngineTests
{
    [Fact]
    public void Interface_ShouldInheritFromIDisposable()
    {
        // Assert
        typeof(IApplicationMigrationEngine).Should().Implement<IDisposable>();
    }

    [Fact]
    public void Interface_ShouldHaveHasRunProperty()
    {
        // Assert
        typeof(IApplicationMigrationEngine).GetProperty("HasRun").Should().NotBeNull();
    }

    [Fact]
    public void Interface_ShouldHaveRunMethod()
    {
        // Assert
        typeof(IApplicationMigrationEngine).GetMethod("Run").Should().NotBeNull();
    }

    [Fact]
    public void Interface_ShouldHaveRunAsyncMethod()
    {
        // Assert
        typeof(IApplicationMigrationEngine).GetMethod("RunAsync").Should().NotBeNull();
    }

    [Fact]
    public void MockImplementation_ShouldWork()
    {
        // Arrange
        var mock = new Mock<IApplicationMigrationEngine>();
        mock.Setup(x => x.HasRun).Returns(true);
        mock.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);

        // Act
        var engine = mock.Object;

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task MockImplementation_RunAsync_ShouldBeAwaitable()
    {
        // Arrange
        var mock = new Mock<IApplicationMigrationEngine>();
        mock.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);

        // Act
        await mock.Object.RunAsync();

        // Assert
        mock.Verify(x => x.RunAsync(), Times.Once);
    }

    [Fact]
    public void MockImplementation_Run_ShouldBeCallable()
    {
        // Arrange
        var mock = new Mock<IApplicationMigrationEngine>();

        // Act
        mock.Object.Run();

        // Assert
        mock.Verify(x => x.Run(), Times.Once);
    }

    [Fact]
    public void MockImplementation_Dispose_ShouldBeCallable()
    {
        // Arrange
        var mock = new Mock<IApplicationMigrationEngine>();

        // Act
        mock.Object.Dispose();

        // Assert
        mock.Verify(x => x.Dispose(), Times.Once);
    }
}
