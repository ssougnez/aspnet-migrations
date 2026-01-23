namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Extensions;
using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Models;
using AreaProg.AspNetCore.Migrations.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class ApplicationBuilderExtensionsTests : IDisposable
{
    public ApplicationBuilderExtensionsTests()
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
    public void UseMigrations_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        var result = appBuilderMock.Object.UseMigrations();

        // Assert
        result.Should().BeSameAs(appBuilderMock.Object);
    }

    [Fact]
    public void UseMigrations_ShouldCallRun()
    {
        // Arrange
        var engineMock = new Mock<IApplicationMigrationEngine>();
        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        appBuilderMock.Object.UseMigrations();

        // Assert
        engineMock.Verify(x => x.Run(It.IsAny<UseMigrationsOptions>()), Times.Once);
    }

    [Fact]
    public void UseMigrations_WithoutEngine_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act & Assert
        var action = () => appBuilderMock.Object.UseMigrations();
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task UseMigrationsAsync_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        var result = await appBuilderMock.Object.UseMigrationsAsync();

        // Assert
        result.Should().BeSameAs(appBuilderMock.Object);
    }

    [Fact]
    public async Task UseMigrationsAsync_ShouldCallRunAsync()
    {
        // Arrange
        var engineMock = new Mock<IApplicationMigrationEngine>();
        engineMock.Setup(x => x.RunAsync(It.IsAny<UseMigrationsOptions>())).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        await appBuilderMock.Object.UseMigrationsAsync();

        // Assert
        engineMock.Verify(x => x.RunAsync(It.IsAny<UseMigrationsOptions>()), Times.Once);
    }

    [Fact]
    public async Task UseMigrationsAsync_WithoutEngine_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act & Assert
        var action = async () => await appBuilderMock.Object.UseMigrationsAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void UseMigrations_ShouldSupportChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act & Assert - chaining should work
        var result = appBuilderMock.Object.UseMigrations();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UseMigrationsAsync_ShouldAwaitCompletion()
    {
        // Arrange
        var completedSuccessfully = false;
        var engineMock = new Mock<IApplicationMigrationEngine>();
        engineMock.Setup(x => x.RunAsync(It.IsAny<UseMigrationsOptions>())).Returns(async () =>
        {
            await Task.Delay(10);
            completedSuccessfully = true;
        });

        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        await appBuilderMock.Object.UseMigrationsAsync();

        // Assert
        completedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void UseMigrations_WithRealEngine_ShouldSetHasRun()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        appBuilderMock.Object.UseMigrations();

        // Assert
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task UseMigrationsAsync_WithRealEngine_ShouldSetHasRun()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act
        await appBuilderMock.Object.UseMigrationsAsync();

        // Assert
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public void UseMigrations_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act & Assert
        var action = () =>
        {
            appBuilderMock.Object.UseMigrations();
            appBuilderMock.Object.UseMigrations();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public async Task UseMigrationsAsync_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilderMock = new Mock<IApplicationBuilder>();
        appBuilderMock.Setup(x => x.ApplicationServices).Returns(serviceProvider);

        // Act & Assert
        var action = async () =>
        {
            await appBuilderMock.Object.UseMigrationsAsync();
            await appBuilderMock.Object.UseMigrationsAsync();
        };
        await action.Should().NotThrowAsync();
    }
}
