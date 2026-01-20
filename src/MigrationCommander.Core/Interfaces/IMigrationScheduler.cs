using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for scheduling migrations to run at a future time.
/// </summary>
public interface IMigrationScheduler
{
    /// <summary>
    /// Schedule a single migration for future execution.
    /// </summary>
    /// <param name="environmentId">Target environment ID.</param>
    /// <param name="migrationId">Migration to schedule.</param>
    /// <param name="scheduledAt">When to execute (UTC).</param>
    /// <param name="scheduledBy">User scheduling the migration.</param>
    /// <param name="notes">Optional notes about the schedule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created scheduled migration info.</returns>
    Task<ScheduledMigrationInfo> ScheduleMigrationAsync(
        Guid environmentId,
        string migrationId,
        DateTime scheduledAt,
        string scheduledBy,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule multiple migrations for future execution as a batch.
    /// </summary>
    /// <param name="environmentId">Target environment ID.</param>
    /// <param name="migrationIds">Migrations to schedule in order.</param>
    /// <param name="scheduledAt">When to start execution (UTC).</param>
    /// <param name="scheduledBy">User scheduling the migrations.</param>
    /// <param name="notes">Optional notes about the schedule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created scheduled migration infos.</returns>
    Task<IReadOnlyList<ScheduledMigrationInfo>> ScheduleBatchAsync(
        Guid environmentId,
        IEnumerable<string> migrationIds,
        DateTime scheduledAt,
        string scheduledBy,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a scheduled migration by ID.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scheduled migration info or null if not found.</returns>
    Task<ScheduledMigrationInfo?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all scheduled migrations, optionally filtered by environment.
    /// </summary>
    /// <param name="environmentId">Optional environment filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of scheduled migrations.</returns>
    Task<IReadOnlyList<ScheduledMigrationInfo>> GetScheduledAsync(
        Guid? environmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pending scheduled migrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending scheduled migrations.</returns>
    Task<IReadOnlyList<ScheduledMigrationInfo>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get scheduled migrations that are due for execution.
    /// </summary>
    /// <param name="asOf">Check due as of this time (UTC). Defaults to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of due scheduled migrations.</returns>
    Task<IReadOnlyList<ScheduledMigrationInfo>> GetDueAsync(
        DateTime? asOf = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a scheduled migration.
    /// </summary>
    /// <param name="scheduleId">The schedule ID to cancel.</param>
    /// <param name="cancelledBy">User cancelling the schedule.</param>
    /// <param name="reason">Reason for cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancelled successfully, false if not found or not cancellable.</returns>
    Task<bool> CancelScheduleAsync(
        Guid scheduleId,
        string cancelledBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reschedule a migration to a new time.
    /// </summary>
    /// <param name="scheduleId">The schedule ID to reschedule.</param>
    /// <param name="newScheduledAt">New scheduled time (UTC).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated scheduled migration info or null if not found or not reschedulable.</returns>
    Task<ScheduledMigrationInfo?> RescheduleAsync(
        Guid scheduleId,
        DateTime newScheduledAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a scheduled migration as running.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsRunningAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a scheduled migration as completed.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <param name="success">Whether execution was successful.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsCompletedAsync(
        Guid scheduleId,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a scheduled migration is about to execute.
    /// </summary>
    event EventHandler<ScheduledMigrationExecutingEventArgs>? ScheduledMigrationExecuting;

    /// <summary>
    /// Event raised when a scheduled migration completes.
    /// </summary>
    event EventHandler<ScheduledMigrationExecutedEventArgs>? ScheduledMigrationExecuted;
}

/// <summary>
/// Event args for scheduled migration executing event.
/// </summary>
public class ScheduledMigrationExecutingEventArgs : EventArgs
{
    public Guid ScheduleId { get; init; }
    public string MigrationId { get; init; } = string.Empty;
    public Guid EnvironmentId { get; init; }
    public DateTime ScheduledAt { get; init; }
    public DateTime ExecutingAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event args for scheduled migration executed event.
/// </summary>
public class ScheduledMigrationExecutedEventArgs : EventArgs
{
    public Guid ScheduleId { get; init; }
    public string MigrationId { get; init; } = string.Empty;
    public Guid EnvironmentId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}
