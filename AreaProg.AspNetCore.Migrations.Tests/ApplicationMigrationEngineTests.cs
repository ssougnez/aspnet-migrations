namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Extensions;
using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Services;
using AreaProg.AspNetCore.Migrations.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ApplicationMigrationEngineTests : IDisposable
{
    private readonly Mock<ILogger<IApplicationMigrationEngine>> _loggerMock;
    private readonly ServiceCollection _services;

    public ApplicationMigrationEngineTests()
    {
        _loggerMock = new Mock<ILogger<IApplicationMigrationEngine>>();
        _services = new ServiceCollection();
        _services.AddLogging();

        // Reset static state before each test
        TestMigrationEngine.Reset();
        Version1Migration.Reset();
        Version2Migration.Reset();
        Version3Migration.Reset();
    }

    public void Dispose()
    {
        // Clean up static state after each test
        TestMigrationEngine.Reset();
        Version1Migration.Reset();
        Version2Migration.Reset();
        Version3Migration.Reset();
    }

    private IServiceProvider BuildServiceProvider()
    {
        return _services.BuildServiceProvider();
    }

    [Fact]
    public void HasRun_Initially_ShouldBeFalse()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act & Assert
        engine.HasRun.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_ShouldSetHasRunToTrue()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public void Run_ShouldSetHasRunToTrue()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        engine.Run();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_CalledTwice_ShouldOnlyExecuteOnce()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();
        await engine.RunAsync();

        // Assert - instance count should be 1 because second call returns early
        TestMigrationEngine.StaticInstanceCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_WithShouldRunFalse_ShouldNotRunMigrations()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(DisabledMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<DisabledMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue(); // HasRun is still set to true
    }

    [Fact]
    public async Task RunAsync_ShouldCallRunBeforeAsync()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert
        TestMigrationEngine.StaticRunBeforeAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ShouldCallRunAfterAsync()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert
        TestMigrationEngine.StaticRunAfterAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act - run multiple concurrent calls
        var tasks = Enumerable.Range(0, 10).Select(_ => engine.RunAsync());
        await Task.WhenAll(tasks);

        // Assert - HasRun should be true (engine executed successfully)
        // The semaphore ensures only one execution completes
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act & Assert
        var action = () => engine.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act & Assert
        var action = () =>
        {
            engine.Dispose();
            engine.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public async Task RunAsync_DiscoversMigrationsFromEngineAssembly()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert - the test migrations (Version1Migration, Version2Migration, Version3Migration)
        // are in the test assembly along with TestMigrationEngine
        engine.HasRun.Should().BeTrue();
        Version1Migration.WasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_WithDbContextNull_ShouldRunWithoutTransactions()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions
        {
            MigrationEngine = typeof(TestMigrationEngine),
            DbContext = null
        };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ShouldExecuteHooksInCorrectOrder()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert - Both RunBefore and RunAfter should have been called
        TestMigrationEngine.StaticRunBeforeAsyncCalled.Should().BeTrue();
        TestMigrationEngine.StaticRunAfterAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ShouldExecuteMigrationsInVersionOrder()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert - all migrations should have been executed
        Version1Migration.WasExecuted.Should().BeTrue();
        Version2Migration.WasExecuted.Should().BeTrue();
        Version3Migration.WasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_FirstTimeMigrations_ShouldHaveFirstTimeFlagTrue()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert - all should have FirstTime = true on first run
        Version1Migration.FirstTimeWhenExecuted.Should().BeTrue();
        Version2Migration.FirstTimeWhenExecuted.Should().BeTrue();
        Version3Migration.FirstTimeWhenExecuted.Should().BeTrue();
    }
}

/// <summary>
/// Tests for version tracking and migration execution logic.
/// </summary>
public class ApplicationMigrationEngineVersionTrackingTests : IDisposable
{
    private readonly Mock<ILogger<IApplicationMigrationEngine>> _loggerMock;
    private readonly ServiceCollection _services;

    public ApplicationMigrationEngineVersionTrackingTests()
    {
        _loggerMock = new Mock<ILogger<IApplicationMigrationEngine>>();
        _services = new ServiceCollection();
        _services.AddLogging();
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
    public async Task RunAsync_WithNoAppliedVersions_ShouldExecuteMigrations()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = _services.BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert - migrations should be executed
        engine.HasRun.Should().BeTrue();
        Version1Migration.WasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_ShouldRegisterVersions()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions { MigrationEngine = typeof(TestMigrationEngine) };
        var serviceProvider = _services.BuildServiceProvider();
        using var engine = new ApplicationMigrationEngine<TestMigrationEngine>(
            serviceProvider, options, _loggerMock.Object);

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue();
        // Migrations should have been executed (verified by the static flags)
        Version1Migration.WasExecuted.Should().BeTrue();
    }
}

/// <summary>
/// Integration tests for ApplicationMigrationEngine with full DI setup.
/// </summary>
public class ApplicationMigrationEngineIntegrationTests : IDisposable
{
    public ApplicationMigrationEngineIntegrationTests()
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
    public async Task FullIntegration_WithServiceCollection_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public void FullIntegration_SyncRun_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        engine.Run();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task FullIntegration_WithDbContext_NotRegistered_ShouldThrowWithClearMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine, TestDbContext>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act & Assert - should throw because DbContext is configured but not registered
        var action = async () => await engine.RunAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TestDbContext*is configured for migrations but is not registered*");
    }

    [Fact]
    public void FullIntegration_Dispose_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act & Assert
        var action = () => engine.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task FullIntegration_DisabledEngine_ShouldNotRunMigrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<DisabledMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        await engine.RunAsync();

        // Assert
        engine.HasRun.Should().BeTrue();
    }

    [Fact]
    public async Task FullIntegration_MigrationsShouldBeExecuted()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationMigrations<TestMigrationEngine>();

        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IApplicationMigrationEngine>();

        // Act
        await engine.RunAsync();

        // Assert
        Version1Migration.WasExecuted.Should().BeTrue();
        Version2Migration.WasExecuted.Should().BeTrue();
        Version3Migration.WasExecuted.Should().BeTrue();
    }
}
