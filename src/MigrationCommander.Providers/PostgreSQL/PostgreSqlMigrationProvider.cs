using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Providers.Base;
using Npgsql;

namespace MigrationCommander.Providers.PostgreSQL;

/// <summary>
/// PostgreSQL migration provider implementation.
/// </summary>
public class PostgreSqlMigrationProvider : BaseMigrationProvider
{
    public override ProviderType ProviderType => ProviderType.PostgreSQL;

    public override DbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlMigrationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new PostgreSqlMigrationDbContext(optionsBuilder.Options);
    }

    public override async Task<long> GetTableRowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
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
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = (int)timeout.TotalSeconds;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    protected override string EscapeTableName(string tableName)
    {
        // Handle schema.table format
        var parts = tableName.Split('.');
        return parts.Length == 2
            ? $"\"{parts[0]}\".\"{parts[1]}\""
            : $"\"{tableName}\"";
    }
}

/// <summary>
/// Minimal DbContext for PostgreSQL connection testing.
/// </summary>
internal class PostgreSqlMigrationDbContext : DbContext
{
    public PostgreSqlMigrationDbContext(DbContextOptions<PostgreSqlMigrationDbContext> options)
        : base(options)
    {
    }
}
