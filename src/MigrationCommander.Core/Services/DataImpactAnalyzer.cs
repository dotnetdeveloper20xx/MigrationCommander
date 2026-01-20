using System.Text.RegularExpressions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Services;

/// <summary>
/// Service for analyzing the data impact of migrations.
/// </summary>
public class DataImpactAnalyzer : IDataImpactAnalyzer
{
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IMigrationProviderFactory _providerFactory;

    // Patterns for detecting operations
    private static readonly Regex DropTablePattern = new(@"DROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DropColumnPattern = new(@"ALTER\s+TABLE\s+[\[`""]?(\w+)[\]`""]?\s+DROP\s+COLUMN\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TruncatePattern = new(@"TRUNCATE\s+TABLE\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeletePattern = new(@"DELETE\s+FROM\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AlterColumnPattern = new(@"ALTER\s+TABLE\s+[\[`""]?(\w+)[\]`""]?\s+ALTER\s+COLUMN\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public DataImpactAnalyzer(
        ISqlGenerator sqlGenerator,
        IMigrationProviderFactory providerFactory)
    {
        _sqlGenerator = sqlGenerator;
        _providerFactory = providerFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TableImpact>> AnalyzeApplyImpactAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var sql = await _sqlGenerator.GenerateUpSqlAsync(migrationId, environment.Provider, cancellationToken);
        return await AnalyzeSqlImpactAsync(environment, sql, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TableImpact>> AnalyzeRollbackImpactAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var sql = await _sqlGenerator.GenerateDownSqlAsync(migrationId, environment.Provider, cancellationToken);
        return await AnalyzeSqlImpactAsync(environment, sql, cancellationToken);
    }

    /// <inheritdoc />
    public bool IsDestructive(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return false;

        return DropTablePattern.IsMatch(sql) ||
               DropColumnPattern.IsMatch(sql) ||
               TruncatePattern.IsMatch(sql) ||
               DeletePattern.IsMatch(sql);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAffectedTables(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return Array.Empty<string>();

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract tables from various patterns
        ExtractTablesFromPattern(sql, DropTablePattern, tables, 1);
        ExtractTablesFromPattern(sql, DropColumnPattern, tables, 1);
        ExtractTablesFromPattern(sql, TruncatePattern, tables, 1);
        ExtractTablesFromPattern(sql, DeletePattern, tables, 1);
        ExtractTablesFromPattern(sql, AlterColumnPattern, tables, 1);

        // Also check CREATE TABLE
        var createTablePattern = new Regex(@"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);
        ExtractTablesFromPattern(sql, createTablePattern, tables, 1);

        // ALTER TABLE
        var alterTablePattern = new Regex(@"ALTER\s+TABLE\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);
        ExtractTablesFromPattern(sql, alterTablePattern, tables, 1);

        return tables.Where(t => !IsSystemTable(t)).ToList();
    }

    /// <inheritdoc />
    public async Task<TimeSpan> EstimateDurationAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var impacts = await AnalyzeApplyImpactAsync(environment, migrationId, cancellationToken);
        var totalRows = impacts.Sum(i => i.CurrentRowCount);

        // Base time + time per 1000 rows
        var baseSeconds = 5;
        var secondsPer1000Rows = 0.5;

        var estimatedSeconds = baseSeconds + (totalRows / 1000.0 * secondsPer1000Rows);

        // Add extra time for destructive operations
        if (impacts.Any(i => i.WillDropTable))
        {
            estimatedSeconds += 10;
        }

        return TimeSpan.FromSeconds(Math.Max(estimatedSeconds, 1));
    }

    private async Task<IReadOnlyList<TableImpact>> AnalyzeSqlImpactAsync(
        DatabaseEnvironment environment,
        string sql,
        CancellationToken cancellationToken)
    {
        var impacts = new List<TableImpact>();
        var provider = _providerFactory.GetProvider(environment.Provider);

        // Analyze DROP TABLE statements
        foreach (Match match in DropTablePattern.Matches(sql))
        {
            var tableName = match.Groups[1].Value;
            var rowCount = await GetRowCountSafeAsync(provider, environment.ConnectionString, tableName, cancellationToken);

            impacts.Add(new TableImpact
            {
                TableName = tableName,
                Action = "DROP TABLE",
                CurrentRowCount = rowCount,
                RowsToBeDeleted = rowCount,
                WillDropTable = true
            });
        }

        // Analyze TRUNCATE TABLE statements
        foreach (Match match in TruncatePattern.Matches(sql))
        {
            var tableName = match.Groups[1].Value;
            var rowCount = await GetRowCountSafeAsync(provider, environment.ConnectionString, tableName, cancellationToken);

            impacts.Add(new TableImpact
            {
                TableName = tableName,
                Action = "TRUNCATE TABLE",
                CurrentRowCount = rowCount,
                RowsToBeDeleted = rowCount,
                WillDropTable = false
            });
        }

        // Analyze DELETE FROM statements
        foreach (Match match in DeletePattern.Matches(sql))
        {
            var tableName = match.Groups[1].Value;
            var rowCount = await GetRowCountSafeAsync(provider, environment.ConnectionString, tableName, cancellationToken);

            impacts.Add(new TableImpact
            {
                TableName = tableName,
                Action = "DELETE FROM",
                CurrentRowCount = rowCount,
                RowsToBeDeleted = rowCount, // Worst case estimate
                WillDropTable = false
            });
        }

        // Analyze DROP COLUMN statements
        foreach (Match match in DropColumnPattern.Matches(sql))
        {
            var tableName = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var rowCount = await GetRowCountSafeAsync(provider, environment.ConnectionString, tableName, cancellationToken);

            var existingImpact = impacts.FirstOrDefault(i => i.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (existingImpact != null)
            {
                existingImpact.AffectedColumns.Add(columnName);
                existingImpact.Action = "DROP COLUMN";
            }
            else
            {
                impacts.Add(new TableImpact
                {
                    TableName = tableName,
                    Action = "DROP COLUMN",
                    CurrentRowCount = rowCount,
                    RowsToBeDeleted = 0, // Column drop doesn't delete rows
                    WillDropTable = false,
                    AffectedColumns = new List<string> { columnName }
                });
            }
        }

        // Analyze ALTER COLUMN statements (potential data loss)
        foreach (Match match in AlterColumnPattern.Matches(sql))
        {
            var tableName = match.Groups[1].Value;
            var columnName = match.Groups[2].Value;
            var rowCount = await GetRowCountSafeAsync(provider, environment.ConnectionString, tableName, cancellationToken);

            if (!impacts.Any(i => i.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            {
                impacts.Add(new TableImpact
                {
                    TableName = tableName,
                    Action = "ALTER COLUMN",
                    CurrentRowCount = rowCount,
                    RowsToBeDeleted = 0,
                    WillDropTable = false,
                    AffectedColumns = new List<string> { columnName }
                });
            }
        }

        return impacts;
    }

    private static async Task<long> GetRowCountSafeAsync(
        IMigrationProvider provider,
        string connectionString,
        string tableName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.GetTableRowCountAsync(connectionString, tableName, cancellationToken);
        }
        catch
        {
            // Table might not exist yet
            return 0;
        }
    }

    private static void ExtractTablesFromPattern(string sql, Regex pattern, HashSet<string> tables, int groupIndex)
    {
        foreach (Match match in pattern.Matches(sql))
        {
            if (match.Groups.Count > groupIndex)
            {
                tables.Add(match.Groups[groupIndex].Value);
            }
        }
    }

    private static bool IsSystemTable(string tableName)
    {
        var systemTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__EFMigrationsHistory",
            "sysdiagrams",
            "sys",
            "INFORMATION_SCHEMA"
        };

        return systemTables.Contains(tableName) || tableName.StartsWith("sys", StringComparison.OrdinalIgnoreCase);
    }
}
