using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for executing migrations against databases.
/// </summary>
public interface IMigrationExecutor
{
    /// <summary>
    /// Applies a single migration to an environment.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID to apply.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<ExecutionResult> ApplyMigrationAsync(
        DatabaseEnvironment environment,
        string migrationId,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies multiple migrations in sequence.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationIds">The migration IDs to apply in order.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution results for each migration.</returns>
    Task<IReadOnlyList<ExecutionResult>> ApplyMigrationsAsync(
        DatabaseEnvironment environment,
        IEnumerable<string> migrationIds,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies all pending migrations to an environment.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution results for each migration.</returns>
    Task<IReadOnlyList<ExecutionResult>> ApplyAllPendingAsync(
        DatabaseEnvironment environment,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised before a migration is executed.
    /// </summary>
    event EventHandler<MigrationExecutingEventArgs>? MigrationExecuting;

    /// <summary>
    /// Event raised after a migration is executed.
    /// </summary>
    event EventHandler<MigrationExecutedEventArgs>? MigrationExecuted;

    /// <summary>
    /// Event raised for progress updates during migration.
    /// </summary>
    event EventHandler<MigrationProgressEventArgs>? MigrationProgress;
}
