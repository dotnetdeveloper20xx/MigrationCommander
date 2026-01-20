using MigrationCommander.Core.Models;
using CoreStatus = MigrationCommander.Core.Models.ScheduledMigrationStatus;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing a scheduled migration.
/// </summary>
public class ScheduledMigration
{
    public Guid Id { get; set; }
    public string MigrationId { get; set; } = string.Empty;
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// When the migration is scheduled to run.
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Who scheduled the migration.
    /// </summary>
    public string ScheduledBy { get; set; } = string.Empty;

    /// <summary>
    /// When the schedule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Status of the scheduled migration.
    /// </summary>
    public ScheduledMigrationStatus Status { get; set; }

    /// <summary>
    /// When the migration was executed (if applicable).
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// Result of the execution (if applicable).
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
    /// When it was cancelled (if applicable).
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Reference to the configured database.
    /// </summary>
    public ConfiguredDatabase? Environment { get; set; }

    /// <summary>
    /// Converts to the domain model.
    /// </summary>
    public ScheduledMigrationInfo ToDomainModel()
    {
        return new ScheduledMigrationInfo
        {
            Id = Id,
            MigrationId = MigrationId,
            EnvironmentId = EnvironmentId,
            EnvironmentName = Environment?.DisplayName ?? string.Empty,
            ScheduledAt = ScheduledAt,
            ScheduledBy = ScheduledBy,
            CreatedAt = CreatedAt,
            Status = (CoreStatus)(int)Status,
            ExecutedAt = ExecutedAt,
            ExecutionSuccess = ExecutionSuccess,
            ErrorMessage = ErrorMessage,
            Notes = Notes,
            CancelledBy = CancelledBy,
            CancelledAt = CancelledAt,
            CancellationReason = CancellationReason
        };
    }

    /// <summary>
    /// Creates from a domain model.
    /// </summary>
    public static ScheduledMigration FromDomainModel(ScheduledMigrationInfo info)
    {
        return new ScheduledMigration
        {
            Id = info.Id == Guid.Empty ? Guid.NewGuid() : info.Id,
            MigrationId = info.MigrationId,
            EnvironmentId = info.EnvironmentId,
            ScheduledAt = info.ScheduledAt,
            ScheduledBy = info.ScheduledBy,
            CreatedAt = info.CreatedAt == default ? DateTime.UtcNow : info.CreatedAt,
            Status = (ScheduledMigrationStatus)(int)info.Status,
            ExecutedAt = info.ExecutedAt,
            ExecutionSuccess = info.ExecutionSuccess,
            ErrorMessage = info.ErrorMessage,
            Notes = info.Notes,
            CancelledBy = info.CancelledBy,
            CancelledAt = info.CancelledAt,
            CancellationReason = info.CancellationReason
        };
    }
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
