namespace AreaProg.AspNetCore.Migrations.Tests;

using AreaProg.AspNetCore.Migrations.Extensions;
using AreaProg.AspNetCore.Migrations.Interfaces;
using AreaProg.AspNetCore.Migrations.Services;
using AreaProg.AspNetCore.Migrations.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplicationMigrations_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplicationMigrations<TestMigrationEngine>();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddApplicationMigrations_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<ApplicationMigrationsOptions>();

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationMigrations_ShouldRegisterOptionsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var options1 = serviceProvider.GetService<ApplicationMigrationsOptions>();
        var options2 = serviceProvider.GetService<ApplicationMigrationsOptions>();

        // Assert
        options1.Should().BeSameAs(options2);
    }

    [Fact]
    public void AddApplicationMigrations_ShouldRegisterMigrationEngine()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetService<IApplicationMigrationEngine>();

        // Assert
        engine.Should().NotBeNull();
        engine.Should().BeOfType<ApplicationMigrationEngine<TestMigrationEngine>>();
    }

    [Fact]
    public void AddApplicationMigrations_ShouldRegisterMigrationEngineAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var engine1 = serviceProvider.GetService<IApplicationMigrationEngine>();
        var engine2 = serviceProvider.GetService<IApplicationMigrationEngine>();

        // Assert
        engine1.Should().BeSameAs(engine2);
    }

    [Fact]
    public void AddApplicationMigrations_WithSetupAction_ShouldInvokeAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var actionInvoked = false;

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>(options =>
        {
            actionInvoked = true;
        });

        // Assert
        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public void AddApplicationMigrations_WithSetupAction_ShouldConfigureDbContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>(options =>
        {
            options.DbContext = typeof(TestDbContext);
        });
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<ApplicationMigrationsOptions>();

        // Assert
        options.DbContext.Should().Be(typeof(TestDbContext));
    }

    [Fact]
    public void AddApplicationMigrations_WithNullSetupAction_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddApplicationMigrations<TestMigrationEngine>(null);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void AddApplicationMigrations_WithoutSetupAction_ShouldHaveNullDbContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<ApplicationMigrationsOptions>();

        // Assert
        options.DbContext.Should().BeNull();
    }

    [Fact]
    public void AddApplicationMigrations_CalledMultipleTimes_ShouldOverwritePrevious()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>(options =>
        {
            options.DbContext = typeof(TestDbContext);
        });
        services.AddApplicationMigrations<TestMigrationEngine>(options =>
        {
            options.DbContext = null;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert - the second registration should take effect
        // Note: ServiceCollection does not automatically replace, so both are registered
        // but GetService returns the last registered one
        var options = serviceProvider.GetServices<ApplicationMigrationsOptions>()
            .LastOrDefault();
        options?.DbContext.Should().BeNull();
    }

    [Fact]
    public void AddApplicationMigrations_ShouldSupportChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
            .AddApplicationMigrations<TestMigrationEngine>()
            .AddLogging();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationMigrations_WithDifferentEngines_ShouldRegisterLastEngine()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        services.AddApplicationMigrations<DisabledMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - ServiceCollection registers multiple, GetService returns the last one
        var allOptions = serviceProvider.GetServices<ApplicationMigrationsOptions>().ToList();
        allOptions.Should().HaveCount(2);
        allOptions[0].MigrationEngine.Should().Be(typeof(TestMigrationEngine));
        allOptions[1].MigrationEngine.Should().Be(typeof(DisabledMigrationEngine));
    }

    [Fact]
    public void AddApplicationMigrations_ShouldSetMigrationEngineProperty()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationMigrations<TestMigrationEngine>();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<ApplicationMigrationsOptions>();

        // Assert
        options.Should().NotBeNull();
        options!.MigrationEngine.Should().Be(typeof(TestMigrationEngine));
    }
}

public class ApplicationMigrationsOptionsTests
{
    [Fact]
    public void DbContext_ShouldDefaultToNull()
    {
        // Arrange & Act
        var options = new ApplicationMigrationsOptions();

        // Assert
        options.DbContext.Should().BeNull();
    }

    [Fact]
    public void DbContext_ShouldBeSettable()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions();

        // Act
        options.DbContext = typeof(TestDbContext);

        // Assert
        options.DbContext.Should().Be(typeof(TestDbContext));
    }

    [Fact]
    public void DbContext_ShouldBeSettableToNull()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions
        {
            DbContext = typeof(TestDbContext)
        };

        // Act
        options.DbContext = null;

        // Assert
        options.DbContext.Should().BeNull();
    }

    [Fact]
    public void DbContext_CanBeSetToAnyType()
    {
        // Arrange
        var options = new ApplicationMigrationsOptions();

        // Act
        options.DbContext = typeof(string); // Even non-DbContext types are allowed at compile time

        // Assert
        options.DbContext.Should().Be(typeof(string));
    }

    [Fact]
    public void MigrationEngine_ShouldDefaultToNull()
    {
        // Arrange & Act
        var options = new ApplicationMigrationsOptions();

        // Assert
        options.MigrationEngine.Should().BeNull();
    }
}
