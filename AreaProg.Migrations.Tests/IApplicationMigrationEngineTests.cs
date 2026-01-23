namespace AreaProg.Migrations.Tests;

using AreaProg.Migrations.Interfaces;
using AreaProg.Migrations.Models;
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
        // Assert - should have both parameterless and with options overloads
        var methods = typeof(IApplicationMigrationEngine).GetMethods().Where(m => m.Name == "Run").ToArray();
        methods.Should().HaveCount(2);
        methods.Should().Contain(m => m.GetParameters().Length == 0);
        methods.Should().Contain(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(UseMigrationsOptions));
    }

    [Fact]
    public void Interface_ShouldHaveRunAsyncMethod()
    {
        // Assert - should have both parameterless and with options overloads
        var methods = typeof(IApplicationMigrationEngine).GetMethods().Where(m => m.Name == "RunAsync").ToArray();
        methods.Should().HaveCount(2);
        methods.Should().Contain(m => m.GetParameters().Length == 0);
        methods.Should().Contain(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(UseMigrationsOptions));
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

    [Fact]
    public async Task MockImplementation_RunAsyncWithOptions_ShouldBeAwaitable()
    {
        // Arrange
        var mock = new Mock<IApplicationMigrationEngine>();
        mock.Setup(x => x.RunAsync(It.IsAny<UseMigrationsOptions>())).Returns(Task.CompletedTask);

        // Act
        await mock.Object.RunAsync(new UseMigrationsOptions());

        // Assert
        mock.Verify(x => x.RunAsync(It.IsAny<UseMigrationsOptions>()), Times.Once);
    }

    [Fact]
    public void MockImplementation_RunWithOptions_ShouldBeCallable()
    {
        // Arrange
        var mock = new Mock<IApplicationMigrationEngine>();

        // Act
        mock.Object.Run(new UseMigrationsOptions());

        // Assert
        mock.Verify(x => x.Run(It.IsAny<UseMigrationsOptions>()), Times.Once);
    }
}
