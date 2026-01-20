using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for resolving migration dependencies and determining execution order.
/// </summary>
public interface IDependencyResolver
{
    /// <summary>
    /// Gets the dependencies for a specific migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration dependency information.</returns>
    Task<MigrationDependency> GetDependenciesAsync(
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the correct execution order for a set of migrations based on their dependencies.
    /// </summary>
    /// <param name="migrationIds">The migration IDs to order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with execution order or errors.</returns>
    Task<DependencyValidationResult> GetExecutionOrderAsync(
        IEnumerable<string> migrationIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all dependencies for a migration are satisfied in the given environment.
    /// </summary>
    /// <param name="migrationId">The migration ID to validate.</param>
    /// <param name="environmentId">The target environment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all dependencies are satisfied.</returns>
    Task<bool> ValidateDependenciesAsync(
        string migrationId,
        Guid environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all migrations that depend on the given migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of dependent migration IDs.</returns>
    Task<IReadOnlyList<string>> GetDependentMigrationsAsync(
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a dependency between migrations.
    /// </summary>
    /// <param name="migrationId">The migration that has the dependency.</param>
    /// <param name="dependsOnMigrationId">The migration it depends on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddDependencyAsync(
        string migrationId,
        string dependsOnMigrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a dependency between migrations.
    /// </summary>
    /// <param name="migrationId">The migration that has the dependency.</param>
    /// <param name="dependsOnMigrationId">The migration it depends on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDependencyAsync(
        string migrationId,
        string dependsOnMigrationId,
        CancellationToken cancellationToken = default);
}
