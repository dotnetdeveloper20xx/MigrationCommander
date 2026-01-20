using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for analyzing the data impact of migrations.
/// </summary>
public interface IDataImpactAnalyzer
{
    /// <summary>
    /// Analyzes the impact of applying a migration.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of table impacts.</returns>
    Task<IReadOnlyList<TableImpact>> AnalyzeApplyImpactAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes the impact of rolling back a migration.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of table impacts.</returns>
    Task<IReadOnlyList<TableImpact>> AnalyzeRollbackImpactAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a migration is destructive (may cause data loss).
    /// </summary>
    /// <param name="sql">The SQL to analyze.</param>
    /// <returns>True if the migration contains destructive operations.</returns>
    bool IsDestructive(string sql);

    /// <summary>
    /// Gets the list of tables affected by a SQL script.
    /// </summary>
    /// <param name="sql">The SQL to analyze.</param>
    /// <returns>List of affected table names.</returns>
    IReadOnlyList<string> GetAffectedTables(string sql);

    /// <summary>
    /// Estimates the duration of a migration based on row counts and operations.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Estimated duration.</returns>
    Task<TimeSpan> EstimateDurationAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default);
}
