using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for managing database environments.
/// </summary>
public interface IEnvironmentManager
{
    /// <summary>
    /// Gets all configured environments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all environments.</returns>
    Task<IReadOnlyList<DatabaseEnvironment>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an environment by ID.
    /// </summary>
    /// <param name="id">The environment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The environment, or null if not found.</returns>
    Task<DatabaseEnvironment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an environment by name.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The environment, or null if not found.</returns>
    Task<DatabaseEnvironment?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new environment.
    /// </summary>
    /// <param name="environment">The environment to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added environment with generated ID.</returns>
    Task<DatabaseEnvironment> AddAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing environment.
    /// </summary>
    /// <param name="environment">The environment to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an environment.
    /// </summary>
    /// <param name="id">The environment ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to an environment.
    /// </summary>
    /// <param name="environment">The environment to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the migration status for an environment.
    /// </summary>
    /// <param name="id">The environment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets environments by provider type.
    /// </summary>
    /// <param name="provider">The provider type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of environments using the specified provider.</returns>
    Task<IReadOnlyList<DatabaseEnvironment>> GetByProviderAsync(
        ProviderType provider,
        CancellationToken cancellationToken = default);
}
