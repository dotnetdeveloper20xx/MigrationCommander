using MigrationCommander.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Provider-specific implementation for database operations.
/// </summary>
public interface IMigrationProvider
{
    /// <summary>
    /// The provider type this implementation supports.
    /// </summary>
    ProviderType ProviderType { get; }

    /// <summary>
    /// Creates a DbContext configured for this provider.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A configured DbContext.</returns>
    DbContext CreateDbContext(string connectionString);

    /// <summary>
    /// Tests the connection to a database.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, error message if failed.</returns>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets applied migrations from the database.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of applied migration IDs.</returns>
    Task<IReadOnlyList<string>> GetAppliedMigrationsAsync(
        string connectionString,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the row count for a table.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The row count.</returns>
    Task<long> GetTableRowCountAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes SQL against the database.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="timeout">Command timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rows affected.</returns>
    Task<int> ExecuteSqlAsync(
        string connectionString,
        string sql,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating provider instances.
/// </summary>
public interface IMigrationProviderFactory
{
    /// <summary>
    /// Gets a provider instance for the specified type.
    /// </summary>
    /// <param name="providerType">The provider type.</param>
    /// <returns>The provider instance.</returns>
    IMigrationProvider GetProvider(ProviderType providerType);

    /// <summary>
    /// Gets all available providers.
    /// </summary>
    /// <returns>List of available provider types.</returns>
    IReadOnlyList<ProviderType> GetAvailableProviders();
}
