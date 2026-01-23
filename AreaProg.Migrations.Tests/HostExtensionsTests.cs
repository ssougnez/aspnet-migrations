namespace AreaProg.Migrations.Tests;

using AreaProg.Migrations.Extensions;
using AreaProg.AspNetCore.Migrations.Extensions;
using AreaProg.Migrations.Interfaces;
using AreaProg.Migrations.Models;
using AreaProg.Migrations.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

public class HostExtensionsTests : IDisposable
{
    public HostExtensionsTests()
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
    public void RunMigrations_ShouldReturnHost()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        var result = hostMock.Object.RunMigrations();

        // Assert
        result.Should().BeSameAs(hostMock.Object);
    }

    [Fact]
    public void RunMigrations_ShouldCallRun()
    {
        // Arrange
        var engineMock = new Mock<IApplicationMigrationEngine>();
        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        hostMock.Object.RunMigrations();

        // Assert
        engineMock.Verify(x => x.Run(It.IsAny<UseMigrationsOptions>()), Times.Once);
    }

    [Fact]
    public void RunMigrations_WithoutEngine_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act & Assert
        var action = () => hostMock.Object.RunMigrations();
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task RunMigrationsAsync_ShouldReturnHost()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        var result = await hostMock.Object.RunMigrationsAsync();

        // Assert
        result.Should().BeSameAs(hostMock.Object);
    }

    [Fact]
    public async Task RunMigrationsAsync_ShouldCallRunAsync()
    {
        // Arrange
        var engineMock = new Mock<IApplicationMigrationEngine>();
        engineMock.Setup(x => x.RunAsync(It.IsAny<UseMigrationsOptions>())).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        await hostMock.Object.RunMigrationsAsync();

        // Assert
        engineMock.Verify(x => x.RunAsync(It.IsAny<UseMigrationsOptions>()), Times.Once);
    }

    [Fact]
    public async Task RunMigrationsAsync_WithoutEngine_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act & Assert
        var action = async () => await hostMock.Object.RunMigrationsAsync();
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void RunMigrations_ShouldSupportChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act & Assert - chaining should work
        var result = hostMock.Object.RunMigrations();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RunMigrationsAsync_ShouldAwaitCompletion()
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

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        await hostMock.Object.RunMigrationsAsync();

        // Assert
        completedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void RunMigrations_WithRealEngine_ShouldSetHasRun()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        hostMock.Object.RunMigrations();

        // Assert
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task RunMigrationsAsync_WithRealEngine_ShouldSetHasRun()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        await hostMock.Object.RunMigrationsAsync();

        // Assert
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public void RunMigrations_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act & Assert
        var action = () =>
        {
            hostMock.Object.RunMigrations();
            hostMock.Object.RunMigrations();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public async Task RunMigrationsAsync_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act & Assert
        var action = async () =>
        {
            await hostMock.Object.RunMigrationsAsync();
            await hostMock.Object.RunMigrationsAsync();
        };
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void RunMigrations_WithOptions_ShouldPassOptions()
    {
        // Arrange
        UseMigrationsOptions? capturedOptions = null;
        var engineMock = new Mock<IApplicationMigrationEngine>();
        engineMock.Setup(x => x.Run(It.IsAny<UseMigrationsOptions>()))
            .Callback<UseMigrationsOptions>(opts => capturedOptions = opts);

        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        hostMock.Object.RunMigrations(opts =>
        {
            opts.EnforceLatestMigration = true;
        });

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.EnforceLatestMigration.Should().BeTrue();
    }

    [Fact]
    public async Task RunMigrationsAsync_WithOptions_ShouldPassOptions()
    {
        // Arrange
        UseMigrationsOptions? capturedOptions = null;
        var engineMock = new Mock<IApplicationMigrationEngine>();
        engineMock.Setup(x => x.RunAsync(It.IsAny<UseMigrationsOptions>()))
            .Callback<UseMigrationsOptions>(opts => capturedOptions = opts)
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(engineMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var hostMock = new Mock<IHost>();
        hostMock.Setup(x => x.Services).Returns(serviceProvider);

        // Act
        await hostMock.Object.RunMigrationsAsync(opts =>
        {
            opts.EnforceLatestMigration = true;
        });

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.EnforceLatestMigration.Should().BeTrue();
    }
}
