using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Services;

/// <summary>
/// Service for generating SQL preview for migrations.
/// </summary>
public class SqlPreviewGenerator : ISqlGenerator
{
    private readonly IMigrationDiscovery _migrationDiscovery;
    private readonly IMigrationProviderFactory _providerFactory;
    private readonly Dictionary<string, Type> _migrationTypeCache = new();

    // Patterns for detecting destructive operations
    private static readonly Regex DropTablePattern = new(@"\bDROP\s+TABLE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DropColumnPattern = new(@"\bDROP\s+COLUMN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TruncatePattern = new(@"\bTRUNCATE\s+TABLE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeletePattern = new(@"\bDELETE\s+FROM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AlterColumnPattern = new(@"\bALTER\s+COLUMN\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Pattern for extracting table names
    private static readonly Regex TableNamePattern = new(
        @"(?:CREATE|ALTER|DROP|TRUNCATE|INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+(?:TABLE\s+)?(?:IF\s+(?:NOT\s+)?EXISTS\s+)?[\[`""]?(\w+)[\]`""]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SqlPreviewGenerator(
        IMigrationDiscovery migrationDiscovery,
        IMigrationProviderFactory providerFactory)
    {
        _migrationDiscovery = migrationDiscovery;
        _providerFactory = providerFactory;
    }

    /// <inheritdoc />
    public async Task<string> GenerateUpSqlAsync(
        string migrationId,
        ProviderType provider,
        CancellationToken cancellationToken = default)
    {
        var migration = await _migrationDiscovery.GetMigrationByIdAsync(migrationId, cancellationToken);
        if (migration == null)
        {
            throw new MigrationNotFoundException(migrationId);
        }

        // If we already have the SQL cached, return it
        if (!string.IsNullOrEmpty(migration.UpSql))
        {
            return migration.UpSql;
        }

        // Try to generate SQL from migration type
        var sql = await GenerateSqlFromMigrationTypeAsync(migration, "Up", provider, cancellationToken);
        if (!string.IsNullOrEmpty(sql))
        {
            return sql;
        }

        // Fall back to placeholder with analysis
        return GenerateAnalyzedPlaceholderSql(migrationId, "UP", provider, migration);
    }

    /// <inheritdoc />
    public async Task<string> GenerateDownSqlAsync(
        string migrationId,
        ProviderType provider,
        CancellationToken cancellationToken = default)
    {
        var migration = await _migrationDiscovery.GetMigrationByIdAsync(migrationId, cancellationToken);
        if (migration == null)
        {
            throw new MigrationNotFoundException(migrationId);
        }

        // If we already have the SQL cached, return it
        if (!string.IsNullOrEmpty(migration.DownSql))
        {
            return migration.DownSql;
        }

        // Try to generate SQL from migration type
        var sql = await GenerateSqlFromMigrationTypeAsync(migration, "Down", provider, cancellationToken);
        if (!string.IsNullOrEmpty(sql))
        {
            return sql;
        }

        // Fall back to placeholder
        return GenerateAnalyzedPlaceholderSql(migrationId, "DOWN", provider, migration);
    }

    /// <inheritdoc />
    public async Task<SchemaDiff> GenerateSchemaDiffAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var diff = new SchemaDiff();

        // Get the migration SQL
        var upSql = await GenerateUpSqlAsync(migrationId, environment.Provider, cancellationToken);

        // Analyze the SQL to extract schema changes
        AnalyzeSqlForSchemaChanges(upSql, diff);

        return diff;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAllPendingSqlAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var pendingMigrations = await _migrationDiscovery.GetPendingMigrationsAsync(environment, cancellationToken);

        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine($"-- Generated SQL for {pendingMigrations.Count} pending migration(s)");
        sqlBuilder.AppendLine($"-- Environment: {environment.DisplayName} ({environment.Provider})");
        sqlBuilder.AppendLine($"-- Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sqlBuilder.AppendLine();

        foreach (var migration in pendingMigrations)
        {
            sqlBuilder.AppendLine($"-- ============================================");
            sqlBuilder.AppendLine($"-- Migration: {migration.Id}");
            sqlBuilder.AppendLine($"-- Name: {migration.Name}");
            sqlBuilder.AppendLine($"-- ============================================");
            sqlBuilder.AppendLine();

            var sql = await GenerateUpSqlAsync(migration.Id, environment.Provider, cancellationToken);
            sqlBuilder.AppendLine(sql);
            sqlBuilder.AppendLine();
            sqlBuilder.AppendLine(GetBatchSeparator(environment.Provider));
            sqlBuilder.AppendLine();
        }

        return sqlBuilder.ToString();
    }

    /// <summary>
    /// Analyzes SQL and determines if it's destructive.
    /// </summary>
    public bool IsDestructive(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return false;

        return DropTablePattern.IsMatch(sql) ||
               DropColumnPattern.IsMatch(sql) ||
               TruncatePattern.IsMatch(sql) ||
               DeletePattern.IsMatch(sql);
    }

    /// <summary>
    /// Extracts affected table names from SQL.
    /// </summary>
    public IReadOnlyList<string> GetAffectedTables(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return Array.Empty<string>();

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = TableNamePattern.Matches(sql);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var tableName = match.Groups[1].Value;
                // Filter out system tables and common EF tables
                if (!IsSystemTable(tableName))
                {
                    tables.Add(tableName);
                }
            }
        }

        return tables.ToList();
    }

    /// <summary>
    /// Analyzes SQL to extract created, dropped, and modified objects.
    /// </summary>
    public (List<string> Created, List<string> Dropped, List<string> Modified) AnalyzeObjects(string sql)
    {
        var created = new List<string>();
        var dropped = new List<string>();
        var modified = new List<string>();

        if (string.IsNullOrEmpty(sql))
            return (created, dropped, modified);

        // Patterns for different operations
        var createPattern = new Regex(@"CREATE\s+(?:TABLE|INDEX|VIEW|PROCEDURE|FUNCTION)\s+(?:IF\s+NOT\s+EXISTS\s+)?[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);
        var dropPattern = new Regex(@"DROP\s+(?:TABLE|INDEX|VIEW|PROCEDURE|FUNCTION)\s+(?:IF\s+EXISTS\s+)?[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);
        var alterPattern = new Regex(@"ALTER\s+TABLE\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);

        foreach (Match match in createPattern.Matches(sql))
        {
            created.Add(match.Groups[1].Value);
        }

        foreach (Match match in dropPattern.Matches(sql))
        {
            dropped.Add(match.Groups[1].Value);
        }

        foreach (Match match in alterPattern.Matches(sql))
        {
            var tableName = match.Groups[1].Value;
            if (!created.Contains(tableName) && !dropped.Contains(tableName))
            {
                modified.Add(tableName);
            }
        }

        return (created, dropped, modified.Distinct().ToList());
    }

    /// <summary>
    /// Registers a migration type for SQL generation.
    /// </summary>
    public void RegisterMigrationType(string migrationId, Type migrationType)
    {
        _migrationTypeCache[migrationId] = migrationType;
    }

    /// <summary>
    /// Registers all migrations from an assembly.
    /// </summary>
    public void RegisterMigrationsFromAssembly(Assembly assembly)
    {
        var migrationTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Migration).IsAssignableFrom(t))
            .ToList();

        foreach (var migrationType in migrationTypes)
        {
            var migrationAttribute = migrationType.GetCustomAttribute<MigrationAttribute>();
            if (migrationAttribute != null)
            {
                _migrationTypeCache[migrationAttribute.Id] = migrationType;
            }
        }
    }

    private async Task<string?> GenerateSqlFromMigrationTypeAsync(
        MigrationInfo migrationInfo,
        string direction,
        ProviderType provider,
        CancellationToken cancellationToken)
    {
        // Try to get migration type from cache or by type name
        Type? migrationType = null;

        if (_migrationTypeCache.TryGetValue(migrationInfo.Id, out migrationType))
        {
            // Found in cache
        }
        else if (!string.IsNullOrEmpty(migrationInfo.TypeName))
        {
            // Try to load type by name
            try
            {
                // Try from all loaded assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        migrationType = assembly.GetType(migrationInfo.TypeName);
                        if (migrationType != null)
                        {
                            _migrationTypeCache[migrationInfo.Id] = migrationType;
                            break;
                        }
                    }
                    catch
                    {
                        // Ignore errors from individual assemblies
                    }
                }
            }
            catch
            {
                // Could not load type
            }
        }

        if (migrationType == null)
        {
            return null;
        }

        try
        {
            // Create migration instance
            var migration = (Migration?)Activator.CreateInstance(migrationType);
            if (migration == null)
            {
                return null;
            }

            // Get operations from migration
            var operations = GetMigrationOperations(migration, direction);
            if (operations == null || operations.Count == 0)
            {
                return $"-- {direction} migration has no operations defined";
            }

            // Generate SQL from operations
            var sql = GenerateSqlFromOperations(operations, provider);
            return await Task.FromResult(sql);
        }
        catch (Exception ex)
        {
            // Return error as comment
            return $"-- Error generating SQL: {ex.Message}\n-- Migration type: {migrationType.FullName}";
        }
    }

    private static IReadOnlyList<MigrationOperation>? GetMigrationOperations(Migration migration, string direction)
    {
        try
        {
            // Get the Up or Down method
            var methodName = direction == "Up" ? "Up" : "Down";
            var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method == null)
            {
                return null;
            }

            // Create a MigrationBuilder to capture operations
            var migrationBuilder = new MigrationBuilder(activeProvider: null);
            method.Invoke(migration, new object[] { migrationBuilder });

            return migrationBuilder.Operations;
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateSqlFromOperations(IReadOnlyList<MigrationOperation> operations, ProviderType provider)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- Generated SQL for {provider}");
        sb.AppendLine($"-- Operations: {operations.Count}");
        sb.AppendLine();

        foreach (var operation in operations)
        {
            sb.AppendLine($"-- Operation: {operation.GetType().Name}");
            sb.AppendLine(GenerateSqlForOperation(operation, provider));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GenerateSqlForOperation(MigrationOperation operation, ProviderType provider)
    {
        return operation switch
        {
            CreateTableOperation createTable => GenerateCreateTableSql(createTable, provider),
            DropTableOperation dropTable => GenerateDropTableSql(dropTable, provider),
            AddColumnOperation addColumn => GenerateAddColumnSql(addColumn, provider),
            DropColumnOperation dropColumn => GenerateDropColumnSql(dropColumn, provider),
            AlterColumnOperation alterColumn => GenerateAlterColumnSql(alterColumn, provider),
            CreateIndexOperation createIndex => GenerateCreateIndexSql(createIndex, provider),
            DropIndexOperation dropIndex => GenerateDropIndexSql(dropIndex, provider),
            AddForeignKeyOperation addFk => GenerateAddForeignKeySql(addFk, provider),
            DropForeignKeyOperation dropFk => GenerateDropForeignKeySql(dropFk, provider),
            AddPrimaryKeyOperation addPk => GenerateAddPrimaryKeySql(addPk, provider),
            DropPrimaryKeyOperation dropPk => GenerateDropPrimaryKeySql(dropPk, provider),
            RenameTableOperation renameTable => GenerateRenameTableSql(renameTable, provider),
            RenameColumnOperation renameColumn => GenerateRenameColumnSql(renameColumn, provider),
            SqlOperation sqlOp => sqlOp.Sql,
            _ => $"-- Unsupported operation: {operation.GetType().Name}"
        };
    }

    private static string GenerateCreateTableSql(CreateTableOperation op, ProviderType provider)
    {
        var sb = new StringBuilder();
        var tableName = QuoteIdentifier(op.Name, provider);

        sb.AppendLine($"CREATE TABLE {tableName} (");

        var columns = new List<string>();
        foreach (var column in op.Columns)
        {
            var columnDef = $"    {QuoteIdentifier(column.Name, provider)} {GetColumnType(column, provider)}";
            if (!column.IsNullable)
            {
                columnDef += " NOT NULL";
            }
            if (column.DefaultValue != null)
            {
                columnDef += $" DEFAULT {FormatDefaultValue(column.DefaultValue, provider)}";
            }
            columns.Add(columnDef);
        }

        if (op.PrimaryKey != null)
        {
            var pkColumns = string.Join(", ", op.PrimaryKey.Columns.Select(c => QuoteIdentifier(c, provider)));
            columns.Add($"    CONSTRAINT {QuoteIdentifier(op.PrimaryKey.Name, provider)} PRIMARY KEY ({pkColumns})");
        }

        sb.AppendLine(string.Join(",\n", columns));
        sb.AppendLine(");");

        return sb.ToString();
    }

    private static string GenerateDropTableSql(DropTableOperation op, ProviderType provider)
    {
        var tableName = QuoteIdentifier(op.Name, provider);
        return provider switch
        {
            ProviderType.SqlServer => $"DROP TABLE {tableName};",
            ProviderType.PostgreSQL => $"DROP TABLE IF EXISTS {tableName};",
            ProviderType.MySQL => $"DROP TABLE IF EXISTS {tableName};",
            ProviderType.SQLite => $"DROP TABLE IF EXISTS {tableName};",
            _ => $"DROP TABLE {tableName};"
        };
    }

    private static string GenerateAddColumnSql(AddColumnOperation op, ProviderType provider)
    {
        var tableName = QuoteIdentifier(op.Table, provider);
        var columnName = QuoteIdentifier(op.Name, provider);
        var columnType = GetColumnType(op, provider);
        var nullable = op.IsNullable ? "NULL" : "NOT NULL";

        return $"ALTER TABLE {tableName} ADD {columnName} {columnType} {nullable};";
    }

    private static string GenerateDropColumnSql(DropColumnOperation op, ProviderType provider)
    {
        var tableName = QuoteIdentifier(op.Table, provider);
        var columnName = QuoteIdentifier(op.Name, provider);

        return $"ALTER TABLE {tableName} DROP COLUMN {columnName};";
    }

    private static string GenerateAlterColumnSql(AlterColumnOperation op, ProviderType provider)
    {
        var tableName = QuoteIdentifier(op.Table, provider);
        var columnName = QuoteIdentifier(op.Name, provider);
        var columnType = GetColumnType(op, provider);

        return provider switch
        {
            ProviderType.SqlServer => $"ALTER TABLE {tableName} ALTER COLUMN {columnName} {columnType};",
            ProviderType.PostgreSQL => $"ALTER TABLE {tableName} ALTER COLUMN {columnName} TYPE {columnType};",
            ProviderType.MySQL => $"ALTER TABLE {tableName} MODIFY COLUMN {columnName} {columnType};",
            _ => $"ALTER TABLE {tableName} ALTER COLUMN {columnName} {columnType};"
        };
    }

    private static string GenerateCreateIndexSql(CreateIndexOperation op, ProviderType provider)
    {
        var indexName = QuoteIdentifier(op.Name, provider);
        var tableName = QuoteIdentifier(op.Table, provider);
        var columns = string.Join(", ", op.Columns.Select(c => QuoteIdentifier(c, provider)));
        var unique = op.IsUnique ? "UNIQUE " : "";

        return $"CREATE {unique}INDEX {indexName} ON {tableName} ({columns});";
    }

    private static string GenerateDropIndexSql(DropIndexOperation op, ProviderType provider)
    {
        var indexName = QuoteIdentifier(op.Name, provider);
        var tableName = QuoteIdentifier(op.Table ?? "", provider);

        return provider switch
        {
            ProviderType.SqlServer => $"DROP INDEX {indexName} ON {tableName};",
            ProviderType.PostgreSQL => $"DROP INDEX IF EXISTS {indexName};",
            ProviderType.MySQL => $"DROP INDEX {indexName} ON {tableName};",
            _ => $"DROP INDEX {indexName};"
        };
    }

    private static string GenerateAddForeignKeySql(AddForeignKeyOperation op, ProviderType provider)
    {
        var constraintName = QuoteIdentifier(op.Name, provider);
        var tableName = QuoteIdentifier(op.Table, provider);
        var columns = string.Join(", ", op.Columns.Select(c => QuoteIdentifier(c, provider)));
        var principalTable = QuoteIdentifier(op.PrincipalTable, provider);
        var principalColumns = string.Join(", ", op.PrincipalColumns.Select(c => QuoteIdentifier(c, provider)));

        return $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} FOREIGN KEY ({columns}) REFERENCES {principalTable} ({principalColumns});";
    }

    private static string GenerateDropForeignKeySql(DropForeignKeyOperation op, ProviderType provider)
    {
        var constraintName = QuoteIdentifier(op.Name, provider);
        var tableName = QuoteIdentifier(op.Table, provider);

        return $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName};";
    }

    private static string GenerateAddPrimaryKeySql(AddPrimaryKeyOperation op, ProviderType provider)
    {
        var constraintName = QuoteIdentifier(op.Name, provider);
        var tableName = QuoteIdentifier(op.Table, provider);
        var columns = string.Join(", ", op.Columns.Select(c => QuoteIdentifier(c, provider)));

        return $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} PRIMARY KEY ({columns});";
    }

    private static string GenerateDropPrimaryKeySql(DropPrimaryKeyOperation op, ProviderType provider)
    {
        var constraintName = QuoteIdentifier(op.Name, provider);
        var tableName = QuoteIdentifier(op.Table, provider);

        return $"ALTER TABLE {tableName} DROP CONSTRAINT {constraintName};";
    }

    private static string GenerateRenameTableSql(RenameTableOperation op, ProviderType provider)
    {
        var oldName = QuoteIdentifier(op.Name, provider);
        var newName = QuoteIdentifier(op.NewName ?? op.Name, provider);

        return provider switch
        {
            ProviderType.SqlServer => $"EXEC sp_rename '{op.Name}', '{op.NewName}';",
            ProviderType.PostgreSQL => $"ALTER TABLE {oldName} RENAME TO {newName};",
            ProviderType.MySQL => $"RENAME TABLE {oldName} TO {newName};",
            _ => $"ALTER TABLE {oldName} RENAME TO {newName};"
        };
    }

    private static string GenerateRenameColumnSql(RenameColumnOperation op, ProviderType provider)
    {
        var tableName = QuoteIdentifier(op.Table, provider);
        var oldName = QuoteIdentifier(op.Name, provider);
        var newName = QuoteIdentifier(op.NewName, provider);

        return provider switch
        {
            ProviderType.SqlServer => $"EXEC sp_rename '{op.Table}.{op.Name}', '{op.NewName}', 'COLUMN';",
            ProviderType.PostgreSQL => $"ALTER TABLE {tableName} RENAME COLUMN {oldName} TO {newName};",
            ProviderType.MySQL => $"ALTER TABLE {tableName} RENAME COLUMN {oldName} TO {newName};",
            _ => $"ALTER TABLE {tableName} RENAME COLUMN {oldName} TO {newName};"
        };
    }

    private static string QuoteIdentifier(string identifier, ProviderType provider)
    {
        return provider switch
        {
            ProviderType.SqlServer => $"[{identifier}]",
            ProviderType.PostgreSQL => $"\"{identifier}\"",
            ProviderType.MySQL => $"`{identifier}`",
            ProviderType.SQLite => $"\"{identifier}\"",
            _ => identifier
        };
    }

    private static string GetColumnType(AddColumnOperation column, ProviderType provider)
    {
        var clrType = column.ClrType;
        var maxLength = column.MaxLength;

        return MapClrTypeToSqlType(clrType, maxLength, provider);
    }

    private static string GetColumnType(AlterColumnOperation column, ProviderType provider)
    {
        var clrType = column.ClrType;
        var maxLength = column.MaxLength;

        return MapClrTypeToSqlType(clrType, maxLength, provider);
    }

    private static string MapClrTypeToSqlType(Type clrType, int? maxLength, ProviderType provider)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (underlyingType == typeof(string))
        {
            if (maxLength.HasValue)
            {
                return provider switch
                {
                    ProviderType.SqlServer => $"NVARCHAR({maxLength})",
                    ProviderType.PostgreSQL => $"VARCHAR({maxLength})",
                    ProviderType.MySQL => $"VARCHAR({maxLength})",
                    _ => $"VARCHAR({maxLength})"
                };
            }
            return provider switch
            {
                ProviderType.SqlServer => "NVARCHAR(MAX)",
                ProviderType.PostgreSQL => "TEXT",
                ProviderType.MySQL => "TEXT",
                _ => "TEXT"
            };
        }

        if (underlyingType == typeof(int))
            return "INT";
        if (underlyingType == typeof(long))
            return "BIGINT";
        if (underlyingType == typeof(short))
            return "SMALLINT";
        if (underlyingType == typeof(byte))
            return "TINYINT";
        if (underlyingType == typeof(bool))
            return provider == ProviderType.PostgreSQL ? "BOOLEAN" : "BIT";
        if (underlyingType == typeof(decimal))
            return "DECIMAL(18,2)";
        if (underlyingType == typeof(double))
            return provider == ProviderType.PostgreSQL ? "DOUBLE PRECISION" : "FLOAT";
        if (underlyingType == typeof(float))
            return "REAL";
        if (underlyingType == typeof(DateTime))
            return provider == ProviderType.PostgreSQL ? "TIMESTAMP" : "DATETIME";
        if (underlyingType == typeof(DateTimeOffset))
            return "DATETIMEOFFSET";
        if (underlyingType == typeof(TimeSpan))
            return "TIME";
        if (underlyingType == typeof(Guid))
            return provider == ProviderType.PostgreSQL ? "UUID" : "UNIQUEIDENTIFIER";
        if (underlyingType == typeof(byte[]))
            return provider == ProviderType.PostgreSQL ? "BYTEA" : "VARBINARY(MAX)";

        return "VARCHAR(255)";
    }

    private static string FormatDefaultValue(object value, ProviderType provider)
    {
        if (value == null)
            return "NULL";
        if (value is string s)
            return $"'{s.Replace("'", "''")}'";
        if (value is bool b)
            return provider == ProviderType.PostgreSQL ? (b ? "TRUE" : "FALSE") : (b ? "1" : "0");
        if (value is DateTime dt)
            return $"'{dt:yyyy-MM-dd HH:mm:ss}'";

        return value.ToString() ?? "NULL";
    }

    private static string GetBatchSeparator(ProviderType provider)
    {
        return provider switch
        {
            ProviderType.SqlServer => "GO",
            ProviderType.PostgreSQL => ";",
            ProviderType.MySQL => ";",
            _ => ";"
        };
    }

    private void AnalyzeSqlForSchemaChanges(string sql, SchemaDiff diff)
    {
        var (created, dropped, modified) = AnalyzeObjects(sql);

        foreach (var tableName in created)
        {
            diff.TablesToCreate.Add(new TableDiff { TableName = tableName });
        }

        foreach (var tableName in dropped)
        {
            diff.TablesToDrop.Add(new TableDiff { TableName = tableName });
        }

        foreach (var tableName in modified)
        {
            diff.TablesToModify.Add(new TableDiff { TableName = tableName });
        }

        // Detect index changes
        var createIndexPattern = new Regex(@"CREATE\s+(?:UNIQUE\s+)?(?:NONCLUSTERED\s+)?INDEX\s+[\[`""]?(\w+)[\]`""]?\s+ON\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);
        var dropIndexPattern = new Regex(@"DROP\s+INDEX\s+[\[`""]?(\w+)[\]`""]?", RegexOptions.IgnoreCase);

        foreach (Match match in createIndexPattern.Matches(sql))
        {
            diff.IndexesToCreate.Add(new IndexDiff
            {
                IndexName = match.Groups[1].Value,
                TableName = match.Groups[2].Value,
                IsUnique = sql.Substring(0, match.Index).Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            });
        }

        foreach (Match match in dropIndexPattern.Matches(sql))
        {
            diff.IndexesToDrop.Add(new IndexDiff { IndexName = match.Groups[1].Value });
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

    private string GenerateAnalyzedPlaceholderSql(string migrationId, string direction, ProviderType provider, MigrationInfo migrationInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- {direction} Migration: {migrationId}");
        sb.AppendLine($"-- Provider: {provider}");
        sb.AppendLine($"-- Name: {migrationInfo.Name}");

        if (!string.IsNullOrEmpty(migrationInfo.Description))
        {
            sb.AppendLine($"-- Description: {migrationInfo.Description}");
        }

        sb.AppendLine();

        if (migrationInfo.CreatedObjects.Count > 0)
        {
            sb.AppendLine($"-- Objects to create: {string.Join(", ", migrationInfo.CreatedObjects)}");
        }

        if (migrationInfo.DroppedObjects.Count > 0)
        {
            sb.AppendLine($"-- Objects to drop: {string.Join(", ", migrationInfo.DroppedObjects)}");
        }

        if (migrationInfo.ModifiedObjects.Count > 0)
        {
            sb.AppendLine($"-- Objects to modify: {string.Join(", ", migrationInfo.ModifiedObjects)}");
        }

        if (migrationInfo.AffectedTables.Count > 0)
        {
            sb.AppendLine($"-- Affected tables: {string.Join(", ", migrationInfo.AffectedTables)}");
        }

        if (migrationInfo.IsDestructive)
        {
            sb.AppendLine("-- WARNING: This migration contains destructive operations");
        }

        sb.AppendLine();
        sb.AppendLine("-- Note: Register the migration assembly with SqlPreviewGenerator.RegisterMigrationsFromAssembly()");
        sb.AppendLine("-- to generate actual SQL statements from EF Core migrations.");

        return sb.ToString();
    }
}
