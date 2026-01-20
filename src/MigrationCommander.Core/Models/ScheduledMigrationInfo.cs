namespace MigrationCommander.Core.Models;

/// <summary>
/// Domain model representing a scheduled migration.
/// </summary>
public class ScheduledMigrationInfo
{
    /// <summary>
    /// Unique identifier for the scheduled migration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The migration ID to be executed.
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// The target environment ID.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// Display name of the target environment.
    /// </summary>
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>
    /// When the migration is scheduled to run (UTC).
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Who scheduled the migration.
    /// </summary>
    public string ScheduledBy { get; set; } = string.Empty;

    /// <summary>
    /// When the schedule was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Current status of the scheduled migration.
    /// </summary>
    public ScheduledMigrationStatus Status { get; set; }

    /// <summary>
    /// When the migration was executed (UTC), if applicable.
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Result of the execution, if applicable.
    /// </summary>
    public bool? ExecutionSuccess { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Notes about the scheduled migration.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// If cancelled, who cancelled it.
    /// </summary>
    public string? CancelledBy { get; set; }

    /// <summary>
    /// When it was cancelled (UTC), if applicable.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Whether the scheduled time has passed.
    /// </summary>
    public bool IsDue => Status == ScheduledMigrationStatus.Pending && ScheduledAt <= DateTime.UtcNow;

    /// <summary>
    /// Whether this schedule can be cancelled.
    /// </summary>
    public bool CanCancel => Status == ScheduledMigrationStatus.Pending;

    /// <summary>
    /// Whether this schedule can be rescheduled.
    /// </summary>
    public bool CanReschedule => Status == ScheduledMigrationStatus.Pending;
}

/// <summary>
/// Status of a scheduled migration.
/// </summary>
public enum ScheduledMigrationStatus
{
    /// <summary>
    /// Scheduled and waiting for execution time.
    /// </summary>
    Pending,

    /// <summary>
    /// Currently being executed.
    /// </summary>
    Running,

    /// <summary>
    /// Successfully completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Cancelled before execution.
    /// </summary>
    Cancelled
}
