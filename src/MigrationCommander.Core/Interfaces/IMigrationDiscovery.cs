using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for discovering migrations from EF Core DbContext assemblies.
/// </summary>
public interface IMigrationDiscovery
{
    /// <summary>
    /// Discovers all migrations in the target DbContext assembly.
    /// </summary>
    /// <param name="dbContextType">The type of the DbContext to scan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of discovered migrations.</returns>
    Task<IReadOnlyList<MigrationInfo>> DiscoverMigrationsAsync(
        Type dbContextType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets migrations that have been applied to a specific database.
    /// </summary>
    /// <param name="environment">The target database environment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of applied migrations.</returns>
    Task<IReadOnlyList<MigrationInfo>> GetAppliedMigrationsAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets migrations that are pending for a specific database.
    /// </summary>
    /// <param name="environment">The target database environment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending migrations.</returns>
    Task<IReadOnlyList<MigrationInfo>> GetPendingMigrationsAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares migration status across multiple environments.
    /// </summary>
    /// <param name="environments">The environments to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison result showing status across all environments.</returns>
    Task<EnvironmentComparisonResult> CompareEnvironmentsAsync(
        IEnumerable<DatabaseEnvironment> environments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific migration by ID.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration info, or null if not found.</returns>
    Task<MigrationInfo?> GetMigrationByIdAsync(
        string migrationId,
        CancellationToken cancellationToken = default);
}
