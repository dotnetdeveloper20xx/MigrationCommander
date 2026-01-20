using Moq;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Core.Services;

namespace MigrationCommander.Core.Tests;

public class MigrationDiscoveryTests
{
    private readonly Mock<IMigrationProviderFactory> _providerFactoryMock;
    private readonly Mock<IMigrationProvider> _providerMock;
    private readonly MigrationDiscoveryService _service;

    public MigrationDiscoveryTests()
    {
        _providerMock = new Mock<IMigrationProvider>();
        _providerFactoryMock = new Mock<IMigrationProviderFactory>();
        _providerFactoryMock
            .Setup(f => f.GetProvider(It.IsAny<ProviderType>()))
            .Returns(_providerMock.Object);

        _service = new MigrationDiscoveryService(_providerFactoryMock.Object);
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_ReturnsCorrectMigrations()
    {
        // Arrange
        var environment = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            DisplayName = "Test Environment",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };

        var appliedMigrationIds = new List<string>
        {
            "20240101000000_InitialCreate",
            "20240115000000_AddUserTable"
        };

        _providerMock
            .Setup(p => p.GetAppliedMigrationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrationIds);

        // Act
        var result = await _service.GetAppliedMigrationsAsync(environment);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(MigrationStatus.Applied, m.Status));
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_ParsesMigrationIdCorrectly()
    {
        // Arrange
        var environment = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };

        var appliedMigrationIds = new List<string>
        {
            "20240115123456_AddUserTable"
        };

        _providerMock
            .Setup(p => p.GetAppliedMigrationsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrationIds);

        // Act
        var result = await _service.GetAppliedMigrationsAsync(environment);

        // Assert
        var migration = result.First();
        Assert.Equal("20240115123456_AddUserTable", migration.Id);
        Assert.Equal("AddUserTable", migration.Name);
        Assert.Equal(new DateTime(2024, 1, 15, 12, 34, 56), migration.Timestamp);
    }

    [Fact]
    public async Task GetMigrationByIdAsync_ReturnsNullForUnknownMigration()
    {
        // Act
        var result = await _service.GetMigrationByIdAsync("unknown_migration");

        // Assert
        Assert.NotNull(result); // It should parse what it can
        Assert.Equal("unknown_migration", result.Id);
    }

    [Fact]
    public async Task CompareEnvironmentsAsync_IdentifiesSyncIssues()
    {
        // Arrange
        var env1 = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "dev",
            DisplayName = "Development",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=dev;"
        };

        var env2 = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "staging",
            DisplayName = "Staging",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=staging;"
        };

        // Setup different responses based on connection string
        _providerMock
            .Setup(p => p.GetAppliedMigrationsAsync(
                It.Is<string>(s => s.Contains("dev")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "20240101_M1", "20240102_M2" });

        _providerMock
            .Setup(p => p.GetAppliedMigrationsAsync(
                It.Is<string>(s => s.Contains("staging")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "20240101_M1" });

        // Act
        var result = await _service.CompareEnvironmentsAsync(new[] { env1, env2 });

        // Assert
        Assert.Equal(2, result.Environments.Count);
        // One environment has pending migration that the other has applied
        Assert.Contains(result.SyncIssues, i => i.IssueType == SyncIssueType.PartiallyApplied);
    }
}
