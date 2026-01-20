using Moq;
using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Core.Services;

namespace MigrationCommander.Core.Tests;

public class MigrationExecutorTests
{
    private readonly Mock<IMigrationDiscovery> _discoveryMock;
    private readonly Mock<ISqlGenerator> _sqlGeneratorMock;
    private readonly Mock<IMigrationProviderFactory> _providerFactoryMock;
    private readonly Mock<IMigrationProvider> _providerMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly Mock<IMigrationNotifier> _notifierMock;
    private readonly MigrationExecutorService _executor;

    public MigrationExecutorTests()
    {
        _discoveryMock = new Mock<IMigrationDiscovery>();
        _sqlGeneratorMock = new Mock<ISqlGenerator>();
        _providerMock = new Mock<IMigrationProvider>();
        _providerFactoryMock = new Mock<IMigrationProviderFactory>();
        _auditLoggerMock = new Mock<IAuditLogger>();
        _notifierMock = new Mock<IMigrationNotifier>();

        _providerFactoryMock
            .Setup(f => f.GetProvider(It.IsAny<ProviderType>()))
            .Returns(_providerMock.Object);

        _executor = new MigrationExecutorService(
            _discoveryMock.Object,
            _sqlGeneratorMock.Object,
            _providerFactoryMock.Object,
            _auditLoggerMock.Object,
            _notifierMock.Object);
    }

    [Fact]
    public async Task ApplyMigrationAsync_SuccessfulMigration_ReturnsSuccessResult()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var sql = "CREATE TABLE Test (Id INT)";

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = migrationId, Name = "TestMigration" });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sql);

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), sql, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _executor.ApplyMigrationAsync(environment, migrationId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(migrationId, result.MigrationId);
        Assert.Equal(sql, result.ExecutedSql);
        Assert.Equal(1, result.RowsAffected);
    }

    [Fact]
    public async Task ApplyMigrationAsync_MigrationNotFound_ThrowsMigrationNotFoundException()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "unknown_migration";

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigrationInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<MigrationNotFoundException>(
            () => _executor.ApplyMigrationAsync(environment, migrationId));
    }

    [Fact]
    public async Task ApplyMigrationAsync_AlreadyApplied_ThrowsMigrationAlreadyAppliedException()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = migrationId, Name = "TestMigration" });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo> { new MigrationInfo { Id = migrationId } });

        // Act & Assert
        await Assert.ThrowsAsync<MigrationAlreadyAppliedException>(
            () => _executor.ApplyMigrationAsync(environment, migrationId));
    }

    [Fact]
    public async Task ApplyMigrationAsync_DryRun_DoesNotExecuteSql()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var sql = "CREATE TABLE Test (Id INT)";
        var options = new MigrationOptions { DryRun = true };

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = migrationId, Name = "TestMigration" });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sql);

        // Act
        var result = await _executor.ApplyMigrationAsync(environment, migrationId, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.WasDryRun);
        Assert.Equal(sql, result.ExecutedSql);
        _providerMock.Verify(
            p => p.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyMigrationAsync_SendsNotifications()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var sql = "CREATE TABLE Test (Id INT)";

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = migrationId, Name = "TestMigration" });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sql);

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), sql, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _executor.ApplyMigrationAsync(environment, migrationId);

        // Assert
        _notifierMock.Verify(n => n.NotifyMigrationStartedAsync(environment.Id, migrationId), Times.Once);
        _notifierMock.Verify(n => n.NotifyMigrationCompletedAsync(environment.Id, migrationId, It.IsAny<ExecutionResult>()), Times.Once);
        _notifierMock.Verify(n => n.NotifyMigrationProgressAsync(environment.Id, migrationId, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<MigrationPhase>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task ApplyMigrationAsync_ExecutionFails_SendsFailureNotification()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var sql = "CREATE TABLE Test (Id INT)";

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = migrationId, Name = "TestMigration" });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sql);

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), sql, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<MigrationExecutionException>(
            () => _executor.ApplyMigrationAsync(environment, migrationId));

        _notifierMock.Verify(n => n.NotifyMigrationFailedAsync(environment.Id, migrationId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApplyMigrationsAsync_StopOnError_StopsAfterFirstFailure()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationIds = new[] { "M1", "M2", "M3" };
        var options = new MigrationOptions { StopOnError = true };

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync("M1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = "M1", Name = "M1" });

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync("M2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MigrationInfo?)null); // This will cause failure

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync("M1", environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SQL1");

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), "SQL1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var results = await _executor.ApplyMigrationsAsync(environment, migrationIds, options);

        // Assert
        Assert.Equal(2, results.Count); // Only M1 and M2 were attempted
        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
    }

    [Fact]
    public async Task ApplyAllPendingAsync_AppliesAllPendingMigrations()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var pendingMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = "M1", Name = "M1" },
            new MigrationInfo { Id = "M2", Name = "M2" }
        };

        _discoveryMock
            .Setup(d => d.GetPendingMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingMigrations);

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => new MigrationInfo { Id = id, Name = id });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(It.IsAny<string>(), environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync("SQL");

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var results = await _executor.ApplyAllPendingAsync(environment);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public void MigrationExecuting_CanCancelMigration()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var sql = "CREATE TABLE Test (Id INT)";

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationInfo { Id = migrationId, Name = "TestMigration" });

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sql);

        _executor.MigrationExecuting += (sender, args) =>
        {
            args.Cancel = true;
            args.CancelReason = "Test cancellation";
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<MigrationCancelledException>(
            () => _executor.ApplyMigrationAsync(environment, migrationId));
    }

    private DatabaseEnvironment CreateTestEnvironment()
    {
        return new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            DisplayName = "Test Environment",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };
    }
}
