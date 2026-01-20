using Moq;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Core.Services;

namespace MigrationCommander.Core.Tests;

public class DataImpactAnalyzerTests
{
    private readonly Mock<ISqlGenerator> _sqlGeneratorMock;
    private readonly Mock<IMigrationProviderFactory> _providerFactoryMock;
    private readonly Mock<IMigrationProvider> _providerMock;
    private readonly DataImpactAnalyzer _analyzer;

    public DataImpactAnalyzerTests()
    {
        _sqlGeneratorMock = new Mock<ISqlGenerator>();
        _providerMock = new Mock<IMigrationProvider>();
        _providerFactoryMock = new Mock<IMigrationProviderFactory>();

        _providerFactoryMock
            .Setup(f => f.GetProvider(It.IsAny<ProviderType>()))
            .Returns(_providerMock.Object);

        _analyzer = new DataImpactAnalyzer(_sqlGeneratorMock.Object, _providerFactoryMock.Object);
    }

    [Theory]
    [InlineData("DROP TABLE Users", true)]
    [InlineData("ALTER TABLE Users DROP COLUMN Name", true)]
    [InlineData("TRUNCATE TABLE Logs", true)]
    [InlineData("DELETE FROM Users WHERE Id > 100", true)]
    [InlineData("CREATE TABLE NewTable (Id INT)", false)]
    [InlineData("ALTER TABLE Users ADD Email VARCHAR(255)", false)]
    public void IsDestructive_CorrectlyIdentifiesDestructiveOperations(string sql, bool expected)
    {
        // Act
        var result = _analyzer.IsDestructive(sql);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetAffectedTables_ReturnsUniqueTableNames()
    {
        // Arrange
        var sql = @"
            DROP TABLE Users;
            ALTER TABLE Users ADD Column1 INT;
            TRUNCATE TABLE Logs;
        ";

        // Act
        var result = _analyzer.GetAffectedTables(sql);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Users", result);
        Assert.Contains("Logs", result);
    }

    [Fact]
    public async Task AnalyzeApplyImpactAsync_ReturnsTableImpacts()
    {
        // Arrange
        var environment = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };

        var sql = "DROP TABLE OldUsers; TRUNCATE TABLE Logs;";

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(It.IsAny<string>(), It.IsAny<ProviderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sql);

        _providerMock
            .Setup(p => p.GetTableRowCountAsync(It.IsAny<string>(), "OldUsers", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000);

        _providerMock
            .Setup(p => p.GetTableRowCountAsync(It.IsAny<string>(), "Logs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5000);

        // Act
        var result = await _analyzer.AnalyzeApplyImpactAsync(environment, "test_migration");

        // Assert
        Assert.Equal(2, result.Count);

        var oldUsersImpact = result.First(i => i.TableName == "OldUsers");
        Assert.Equal("DROP TABLE", oldUsersImpact.Action);
        Assert.Equal(1000, oldUsersImpact.CurrentRowCount);
        Assert.True(oldUsersImpact.WillDropTable);

        var logsImpact = result.First(i => i.TableName == "Logs");
        Assert.Equal("TRUNCATE TABLE", logsImpact.Action);
        Assert.Equal(5000, logsImpact.CurrentRowCount);
    }

    [Fact]
    public async Task EstimateDurationAsync_ReturnsReasonableEstimate()
    {
        // Arrange
        var environment = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };

        _sqlGeneratorMock
            .Setup(g => g.GenerateUpSqlAsync(It.IsAny<string>(), It.IsAny<ProviderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("DROP TABLE LargeTable");

        _providerMock
            .Setup(p => p.GetTableRowCountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100000);

        // Act
        var result = await _analyzer.EstimateDurationAsync(environment, "test_migration");

        // Assert
        Assert.True(result.TotalSeconds > 0);
        Assert.True(result.TotalMinutes < 10); // Should be reasonable
    }

    [Fact]
    public async Task AnalyzeRollbackImpactAsync_UsesDownSql()
    {
        // Arrange
        var environment = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };

        _sqlGeneratorMock
            .Setup(g => g.GenerateDownSqlAsync(It.IsAny<string>(), It.IsAny<ProviderType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("DROP TABLE NewTable");

        _providerMock
            .Setup(p => p.GetTableRowCountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);

        // Act
        var result = await _analyzer.AnalyzeRollbackImpactAsync(environment, "test_migration");

        // Assert
        _sqlGeneratorMock.Verify(g => g.GenerateDownSqlAsync(It.IsAny<string>(), It.IsAny<ProviderType>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
