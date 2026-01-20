namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents a batch of migrations to be executed together.
/// </summary>
public class BatchMigrationJob
{
    /// <summary>
    /// Unique identifier for this batch job.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// List of migration IDs to execute in order.
    /// </summary>
    public List<string> MigrationIds { get; set; } = new();

    /// <summary>
    /// Target environment ID.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// Execution mode for the batch.
    /// </summary>
    public BatchExecutionMode Mode { get; set; } = BatchExecutionMode.Sequential;

    /// <summary>
    /// Migration options to apply to all migrations in the batch.
    /// </summary>
    public MigrationOptions? Options { get; set; }

    /// <summary>
    /// When the batch job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created this batch job.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Notes about this batch job.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Defines how migrations in a batch should be executed.
/// </summary>
public enum BatchExecutionMode
{
    /// <summary>
    /// Execute migrations one at a time in order.
    /// </summary>
    Sequential,

    /// <summary>
    /// Stop execution if any migration fails (default behavior).
    /// </summary>
    StopOnFirstError,

    /// <summary>
    /// Continue executing remaining migrations even if one fails.
    /// </summary>
    ContinueOnError
}
