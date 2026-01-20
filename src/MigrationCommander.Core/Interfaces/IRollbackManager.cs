using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for managing migration rollbacks.
/// </summary>
public interface IRollbackManager
{
    /// <summary>
    /// Analyzes the impact of rolling back a migration.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis of the rollback impact.</returns>
    Task<RollbackAnalysis> AnalyzeRollbackAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a rollback for a single migration.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID to roll back.</param>
    /// <param name="options">Optional rollback options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<ExecutionResult> RollbackMigrationAsync(
        DatabaseEnvironment environment,
        string migrationId,
        RollbackOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back to a specific migration (rolls back all migrations after it).
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="targetMigrationId">The migration to roll back to (this migration remains applied).</param>
    /// <param name="options">Optional rollback options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution results for each rolled back migration.</returns>
    Task<IReadOnlyList<ExecutionResult>> RollbackToMigrationAsync(
        DatabaseEnvironment environment,
        string targetMigrationId,
        RollbackOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all applied migrations.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="options">Optional rollback options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution results for each rolled back migration.</returns>
    Task<IReadOnlyList<ExecutionResult>> RollbackAllAsync(
        DatabaseEnvironment environment,
        RollbackOptions? options = null,
        CancellationToken cancellationToken = default);
}
