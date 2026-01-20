using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Providers.Base;
using MySqlConnector;

namespace MigrationCommander.Providers.MySQL;

/// <summary>
/// MySQL migration provider implementation.
/// </summary>
public class MySqlMigrationProvider : BaseMigrationProvider
{
    public override ProviderType ProviderType => ProviderType.MySQL;

    public override DbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MySqlMigrationDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new MySqlMigrationDbContext(optionsBuilder.Options);
    }

    public override async Task<long> GetTableRowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
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
        await using var connection = new MySqlConnection(connectionString);
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
            ? $"`{parts[0]}`.`{parts[1]}`"
            : $"`{tableName}`";
    }
}

/// <summary>
/// Minimal DbContext for MySQL connection testing.
/// </summary>
internal class MySqlMigrationDbContext : DbContext
{
    public MySqlMigrationDbContext(DbContextOptions<MySqlMigrationDbContext> options)
        : base(options)
    {
    }
}
