using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Interface for notifying clients about migration status changes.
/// </summary>
public interface IMigrationNotifier
{
    /// <summary>
    /// Notifies that a migration has started.
    /// </summary>
    Task NotifyMigrationStartedAsync(Guid environmentId, string migrationId);

    /// <summary>
    /// Notifies about migration progress.
    /// </summary>
    Task NotifyMigrationProgressAsync(Guid environmentId, string migrationId, int percentage, string message, MigrationPhase phase);

    /// <summary>
    /// Notifies that a migration has completed successfully.
    /// </summary>
    Task NotifyMigrationCompletedAsync(Guid environmentId, string migrationId, ExecutionResult result);

    /// <summary>
    /// Notifies that a migration has failed.
    /// </summary>
    Task NotifyMigrationFailedAsync(Guid environmentId, string migrationId, string errorMessage);

    /// <summary>
    /// Notifies that a rollback has started.
    /// </summary>
    Task NotifyRollbackStartedAsync(Guid environmentId, string migrationId);

    /// <summary>
    /// Notifies that a rollback has completed.
    /// </summary>
    Task NotifyRollbackCompletedAsync(Guid environmentId, string migrationId, ExecutionResult result);

    /// <summary>
    /// Notifies of a general status update.
    /// </summary>
    Task NotifyStatusUpdateAsync(Guid environmentId, string status, string message);
}

/// <summary>
/// Null implementation of IMigrationNotifier for when SignalR is not enabled.
/// </summary>
public class NullMigrationNotifier : IMigrationNotifier
{
    public Task NotifyMigrationStartedAsync(Guid environmentId, string migrationId) => Task.CompletedTask;
    public Task NotifyMigrationProgressAsync(Guid environmentId, string migrationId, int percentage, string message, MigrationPhase phase) => Task.CompletedTask;
    public Task NotifyMigrationCompletedAsync(Guid environmentId, string migrationId, ExecutionResult result) => Task.CompletedTask;
    public Task NotifyMigrationFailedAsync(Guid environmentId, string migrationId, string errorMessage) => Task.CompletedTask;
    public Task NotifyRollbackStartedAsync(Guid environmentId, string migrationId) => Task.CompletedTask;
    public Task NotifyRollbackCompletedAsync(Guid environmentId, string migrationId, ExecutionResult result) => Task.CompletedTask;
    public Task NotifyStatusUpdateAsync(Guid environmentId, string status, string message) => Task.CompletedTask;
}
