using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Providers.Base;

namespace MigrationCommander.Providers.SqlServer;

/// <summary>
/// SQL Server migration provider implementation.
/// </summary>
public class SqlServerMigrationProvider : BaseMigrationProvider
{
    public override ProviderType ProviderType => ProviderType.SqlServer;

    public override DbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlServerMigrationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new SqlServerMigrationDbContext(optionsBuilder.Options);
    }

    public override async Task<long> GetTableRowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
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
        await using var connection = new SqlConnection(connectionString);
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
            ? $"[{parts[0]}].[{parts[1]}]"
            : $"[{tableName}]";
    }
}

/// <summary>
/// Minimal DbContext for SQL Server connection testing.
/// </summary>
internal class SqlServerMigrationDbContext : DbContext
{
    public SqlServerMigrationDbContext(DbContextOptions<SqlServerMigrationDbContext> options)
        : base(options)
    {
    }
}
