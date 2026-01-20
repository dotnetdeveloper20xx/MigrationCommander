using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Providers.Base;

/// <summary>
/// Base class for database migration providers.
/// </summary>
public abstract class BaseMigrationProvider : IMigrationProvider
{
    /// <inheritdoc />
    public abstract ProviderType ProviderType { get; }

    /// <inheritdoc />
    public abstract DbContext CreateDbContext(string connectionString);

    /// <inheritdoc />
    public virtual async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = CreateDbContext(connectionString);
            await context.Database.CanConnectAsync(cancellationToken);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        using var context = CreateDbContext(connectionString);
        var appliedMigrations = await context.Database
            .GetAppliedMigrationsAsync(cancellationToken);
        return appliedMigrations.ToList();
    }

    /// <inheritdoc />
    public abstract Task<long> GetTableRowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<int> ExecuteSqlAsync(
        string connectionString,
        string sql,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the SQL for counting rows in a table.
    /// Override in provider-specific implementations if needed.
    /// </summary>
    protected virtual string GetCountSql(string tableName)
    {
        return $"SELECT COUNT(*) FROM {EscapeTableName(tableName)}";
    }

    /// <summary>
    /// Escapes a table name for use in SQL queries.
    /// Override in provider-specific implementations.
    /// </summary>
    protected abstract string EscapeTableName(string tableName);
}
