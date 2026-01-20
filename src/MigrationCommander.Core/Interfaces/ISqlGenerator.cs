using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for generating SQL for migrations.
/// </summary>
public interface ISqlGenerator
{
    /// <summary>
    /// Generates the UP SQL for a migration targeting a specific provider.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="provider">The target database provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated UP SQL.</returns>
    Task<string> GenerateUpSqlAsync(
        string migrationId,
        ProviderType provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the DOWN SQL for a migration targeting a specific provider.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="provider">The target database provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated DOWN SQL.</returns>
    Task<string> GenerateDownSqlAsync(
        string migrationId,
        ProviderType provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a schema diff between current schema and migration target.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The schema difference.</returns>
    Task<SchemaDiff> GenerateSchemaDiffAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates SQL for all pending migrations.
    /// </summary>
    /// <param name="environment">The target environment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined SQL for all pending migrations.</returns>
    Task<string> GenerateAllPendingSqlAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default);
}
