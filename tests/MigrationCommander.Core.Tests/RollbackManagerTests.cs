using Moq;
using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Core.Services;

namespace MigrationCommander.Core.Tests;

public class RollbackManagerTests
{
    private readonly Mock<IMigrationDiscovery> _discoveryMock;
    private readonly Mock<ISqlGenerator> _sqlGeneratorMock;
    private readonly Mock<IDataImpactAnalyzer> _impactAnalyzerMock;
    private readonly Mock<IMigrationProviderFactory> _providerFactoryMock;
    private readonly Mock<IMigrationProvider> _providerMock;
    private readonly Mock<IAuditLogger> _auditLoggerMock;
    private readonly Mock<IMigrationNotifier> _notifierMock;
    private readonly RollbackManager _rollbackManager;

    public RollbackManagerTests()
    {
        _discoveryMock = new Mock<IMigrationDiscovery>();
        _sqlGeneratorMock = new Mock<ISqlGenerator>();
        _impactAnalyzerMock = new Mock<IDataImpactAnalyzer>();
        _providerMock = new Mock<IMigrationProvider>();
        _providerFactoryMock = new Mock<IMigrationProviderFactory>();
        _auditLoggerMock = new Mock<IAuditLogger>();
        _notifierMock = new Mock<IMigrationNotifier>();

        _providerFactoryMock
            .Setup(f => f.GetProvider(It.IsAny<ProviderType>()))
            .Returns(_providerMock.Object);

        _rollbackManager = new RollbackManager(
            _discoveryMock.Object,
            _sqlGeneratorMock.Object,
            _impactAnalyzerMock.Object,
            _providerFactoryMock.Object,
            _auditLoggerMock.Object,
            _notifierMock.Object);
    }

    [Fact]
    public async Task AnalyzeRollbackAsync_MigrationNotApplied_ReturnsCannotRollback()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        // Act
        var result = await _rollbackManager.AnalyzeRollbackAsync(environment, migrationId);

        // Assert
        Assert.False(result.CanRollback);
        Assert.Equal(RollbackRiskLevel.Low, result.RiskLevel);
        Assert.Contains("not been applied", result.BlockingReason);
    }

    [Fact]
    public async Task AnalyzeRollbackAsync_HasDependentMigrations_ReturnsCannotRollback()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_FirstMigration";

        var appliedMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = migrationId, Name = "FirstMigration", Timestamp = new DateTime(2024, 1, 1) },
            new MigrationInfo { Id = "20240102000000_SecondMigration", Name = "SecondMigration", Timestamp = new DateTime(2024, 1, 2) }
        };

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrations);

        // Act
        var result = await _rollbackManager.AnalyzeRollbackAsync(environment, migrationId);

        // Assert
        Assert.False(result.CanRollback);
        Assert.Equal(RollbackRiskLevel.High, result.RiskLevel);
        Assert.Contains("must be rolled back first", result.BlockingReason);
        Assert.Single(result.DependentMigrations);
    }

    [Fact]
    public async Task AnalyzeRollbackAsync_WillDropTables_ReturnsCriticalRisk()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var downSql = "DROP TABLE Users";

        var appliedMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = migrationId, Name = "TestMigration", Timestamp = new DateTime(2024, 1, 1) }
        };

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrations);

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downSql);

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>
            {
                new TableImpact { TableName = "Users", Action = "DROP TABLE", WillDropTable = true, CurrentRowCount = 1000 }
            });

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(5));

        // Act
        var result = await _rollbackManager.AnalyzeRollbackAsync(environment, migrationId);

        // Assert
        Assert.True(result.CanRollback);
        Assert.Equal(RollbackRiskLevel.Critical, result.RiskLevel);
        Assert.True(result.WillDropTables);
        Assert.Contains(result.Warnings, w => w.Contains("DROP"));
    }

    [Fact]
    public async Task AnalyzeRollbackAsync_ProductionEnvironment_AddsWarning()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        environment.IsProduction = true;
        var migrationId = "20240101000000_TestMigration";

        var appliedMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = migrationId, Name = "TestMigration", Timestamp = new DateTime(2024, 1, 1) }
        };

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrations);

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ALTER TABLE Users ADD Column1 INT");

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>());

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(1));

        // Act
        var result = await _rollbackManager.AnalyzeRollbackAsync(environment, migrationId);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("PRODUCTION"));
    }

    [Fact]
    public async Task RollbackMigrationAsync_SuccessfulRollback_ReturnsSuccessResult()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var downSql = "DROP TABLE NewTable";
        var options = new RollbackOptions { Force = true };

        var appliedMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = migrationId, Name = "TestMigration", Timestamp = new DateTime(2024, 1, 1) }
        };

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrations);

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downSql);

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>());

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(1));

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), downSql, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _rollbackManager.RollbackMigrationAsync(environment, migrationId, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(migrationId, result.MigrationId);
        Assert.Equal(downSql, result.ExecutedSql);
    }

    [Fact]
    public async Task RollbackMigrationAsync_HighRiskWithoutForce_ThrowsException()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var options = new RollbackOptions { Force = false }; // No force

        var appliedMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = migrationId, Name = "TestMigration", Timestamp = new DateTime(2024, 1, 1) }
        };

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrations);

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync("DROP TABLE Users");

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>
            {
                new TableImpact { TableName = "Users", Action = "DROP TABLE", WillDropTable = true }
            });

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(1));

        // Act & Assert
        await Assert.ThrowsAsync<RollbackNotPossibleException>(
            () => _rollbackManager.RollbackMigrationAsync(environment, migrationId, options));
    }

    [Fact]
    public async Task RollbackMigrationAsync_SendsNotifications()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var migrationId = "20240101000000_TestMigration";
        var downSql = "DROP TABLE NewTable";
        var options = new RollbackOptions { Force = true };

        var appliedMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = migrationId, Name = "TestMigration", Timestamp = new DateTime(2024, 1, 1) }
        };

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appliedMigrations);

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(migrationId, environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downSql);

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>());

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, migrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(1));

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), downSql, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _rollbackManager.RollbackMigrationAsync(environment, migrationId, options);

        // Assert
        _notifierMock.Verify(n => n.NotifyRollbackStartedAsync(environment.Id, migrationId), Times.Once);
        _notifierMock.Verify(n => n.NotifyRollbackCompletedAsync(environment.Id, migrationId, It.IsAny<ExecutionResult>()), Times.Once);
    }

    [Fact]
    public async Task RollbackToMigrationAsync_RollsBackMultipleMigrations()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var targetMigrationId = "20240101000000_M1";

        // Track which migrations have been rolled back
        var rolledBackMigrations = new HashSet<string>();

        var allMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = "20240101000000_M1", Name = "M1", Timestamp = new DateTime(2024, 1, 1) },
            new MigrationInfo { Id = "20240102000000_M2", Name = "M2", Timestamp = new DateTime(2024, 1, 2) },
            new MigrationInfo { Id = "20240103000000_M3", Name = "M3", Timestamp = new DateTime(2024, 1, 3) }
        };

        // Dynamically return the list of applied migrations minus rolled back ones
        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => allMigrations.Where(m => !rolledBackMigrations.Contains(m.Id)).ToList());

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(It.IsAny<string>(), environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync("DROP TABLE Test");

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>());

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(1));

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, TimeSpan, CancellationToken>((conn, sql, timeout, ct) =>
            {
                // Simulate rollback by tracking which migration was rolled back
                // We need to track this based on the call order
            })
            .ReturnsAsync(1);

        // Track rollbacks through the notifier
        _notifierMock
            .Setup(n => n.NotifyRollbackCompletedAsync(environment.Id, It.IsAny<string>(), It.IsAny<ExecutionResult>()))
            .Callback<Guid, string, ExecutionResult>((envId, migId, result) => rolledBackMigrations.Add(migId))
            .Returns(Task.CompletedTask);

        // Act
        var results = await _rollbackManager.RollbackToMigrationAsync(environment, targetMigrationId);

        // Assert
        Assert.Equal(2, results.Count); // M3 and M2 should be rolled back (not M1)
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task RollbackToMigrationAsync_TargetNotApplied_ThrowsException()
    {
        // Arrange
        var environment = CreateTestEnvironment();
        var targetMigrationId = "non_existent_migration";

        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MigrationInfo>());

        // Act & Assert
        await Assert.ThrowsAsync<MigrationNotAppliedException>(
            () => _rollbackManager.RollbackToMigrationAsync(environment, targetMigrationId));
    }

    [Fact]
    public async Task RollbackAllAsync_RollsBackAllMigrations()
    {
        // Arrange
        var environment = CreateTestEnvironment();

        // Track which migrations have been rolled back
        var rolledBackMigrations = new HashSet<string>();

        var allMigrations = new List<MigrationInfo>
        {
            new MigrationInfo { Id = "20240101000000_M1", Name = "M1", Timestamp = new DateTime(2024, 1, 1) },
            new MigrationInfo { Id = "20240102000000_M2", Name = "M2", Timestamp = new DateTime(2024, 1, 2) }
        };

        // Dynamically return the list of applied migrations minus rolled back ones
        _discoveryMock
            .Setup(d => d.GetAppliedMigrationsAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => allMigrations.Where(m => !rolledBackMigrations.Contains(m.Id)).ToList());

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(It.IsAny<string>(), environment.Provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync("DROP TABLE Test");

        _impactAnalyzerMock
            .Setup(a => a.AnalyzeRollbackImpactAsync(environment, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableImpact>());

        _impactAnalyzerMock
            .Setup(a => a.EstimateDurationAsync(environment, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(1));

        _providerMock
            .Setup(p => p.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Track rollbacks through the notifier
        _notifierMock
            .Setup(n => n.NotifyRollbackCompletedAsync(environment.Id, It.IsAny<string>(), It.IsAny<ExecutionResult>()))
            .Callback<Guid, string, ExecutionResult>((envId, migId, result) => rolledBackMigrations.Add(migId))
            .Returns(Task.CompletedTask);

        // Act
        var results = await _rollbackManager.RollbackAllAsync(environment);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
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
