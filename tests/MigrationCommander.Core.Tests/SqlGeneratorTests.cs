using Moq;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Core.Services;

namespace MigrationCommander.Core.Tests;

public class SqlGeneratorTests
{
    private readonly Mock<IMigrationDiscovery> _discoveryMock;
    private readonly Mock<IMigrationProviderFactory> _providerFactoryMock;
    private readonly SqlPreviewGenerator _generator;

    public SqlGeneratorTests()
    {
        _discoveryMock = new Mock<IMigrationDiscovery>();
        _providerFactoryMock = new Mock<IMigrationProviderFactory>();

        _generator = new SqlPreviewGenerator(_discoveryMock.Object, _providerFactoryMock.Object);
    }

    [Theory]
    [InlineData("DROP TABLE Users", true)]
    [InlineData("DROP COLUMN Name", true)]
    [InlineData("TRUNCATE TABLE Logs", true)]
    [InlineData("DELETE FROM Users", true)]
    [InlineData("CREATE TABLE Users (Id INT)", false)]
    [InlineData("ALTER TABLE Users ADD Name VARCHAR(100)", false)]
    [InlineData("CREATE INDEX IX_Users_Name ON Users(Name)", false)]
    public void IsDestructive_CorrectlyIdentifiesDestructiveOperations(string sql, bool expectedResult)
    {
        // Act
        var result = _generator.IsDestructive(sql);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("CREATE TABLE Users (Id INT)", new[] { "Users" })]
    [InlineData("ALTER TABLE Products ADD Price DECIMAL", new[] { "Products" })]
    [InlineData("DROP TABLE Orders", new[] { "Orders" })]
    [InlineData("CREATE TABLE Users (Id INT); ALTER TABLE Products ADD Price DECIMAL", new[] { "Users", "Products" })]
    public void GetAffectedTables_ExtractsTableNamesCorrectly(string sql, string[] expectedTables)
    {
        // Act
        var result = _generator.GetAffectedTables(sql);

        // Assert
        Assert.Equal(expectedTables.Length, result.Count);
        foreach (var table in expectedTables)
        {
            Assert.Contains(table, result);
        }
    }

    [Fact]
    public void GetAffectedTables_IgnoresSystemTables()
    {
        // Arrange
        var sql = "SELECT * FROM __EFMigrationsHistory; CREATE TABLE Users (Id INT)";

        // Act
        var result = _generator.GetAffectedTables(sql);

        // Assert
        Assert.DoesNotContain("__EFMigrationsHistory", result);
        Assert.Contains("Users", result);
    }

    [Fact]
    public void AnalyzeObjects_CorrectlyCategorizesDdlStatements()
    {
        // Arrange
        var sql = @"
            CREATE TABLE Users (Id INT);
            CREATE INDEX IX_Users_Name ON Users(Name);
            ALTER TABLE Products ADD Price DECIMAL;
            DROP TABLE OldLogs;
        ";

        // Act
        var (created, dropped, modified) = _generator.AnalyzeObjects(sql);

        // Assert
        Assert.Contains("Users", created);
        Assert.Contains("OldLogs", dropped);
        Assert.Contains("Products", modified);
    }

    [Fact]
    public async Task GenerateSchemaDiffAsync_ReturnsCorrectDiff()
    {
        // Arrange
        var environment = new DatabaseEnvironment
        {
            Id = Guid.NewGuid(),
            Name = "test",
            Provider = ProviderType.SqlServer,
            ConnectionString = "Server=localhost;Database=test;"
        };

        var migration = new MigrationInfo
        {
            Id = "20240101_Test",
            Name = "Test",
            UpSql = "CREATE TABLE NewTable (Id INT)"
        };

        _discoveryMock
            .Setup(d => d.GetMigrationByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(migration);

        // Act
        var result = await _generator.GenerateSchemaDiffAsync(environment, migration.Id);

        // Assert
        Assert.True(result.HasChanges);
        Assert.Single(result.TablesToCreate);
    }
}
