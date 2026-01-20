using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Providers.Base;

namespace MigrationCommander.Providers.SQLite;

/// <summary>
/// SQLite migration provider implementation.
/// </summary>
public class SqliteMigrationProvider : BaseMigrationProvider
{
    public override ProviderType ProviderType => ProviderType.SQLite;

    public override DbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteMigrationDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new SqliteMigrationDbContext(optionsBuilder.Options);
    }

    public override async Task<long> GetTableRowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = GetCountSql(tableName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public override async Task<int> ExecuteSqlAsync(
        string connectionString,
        string sql,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        // SQLite doesn't support command timeout in the same way

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected override string EscapeTableName(string tableName)
    {
        return $"\"{tableName}\"";
    }
}

/// <summary>
/// Minimal DbContext for SQLite connection testing.
/// </summary>
internal class SqliteMigrationDbContext : DbContext
{
    public SqliteMigrationDbContext(DbContextOptions<SqliteMigrationDbContext> options)
        : base(options)
    {
    }
}
