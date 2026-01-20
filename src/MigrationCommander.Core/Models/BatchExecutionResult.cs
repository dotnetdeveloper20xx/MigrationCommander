namespace MigrationCommander.Core.Models;

/// <summary>
/// Result of executing a batch of migrations.
/// </summary>
public class BatchExecutionResult
{
    /// <summary>
    /// The batch job that was executed.
    /// </summary>
    public Guid BatchJobId { get; set; }

    /// <summary>
    /// Target environment ID.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// Whether all migrations in the batch succeeded.
    /// </summary>
    public bool Success => FailedCount == 0 && Results.All(r => r.Success);

    /// <summary>
    /// Individual results for each migration in the batch.
    /// </summary>
    public List<ExecutionResult> Results { get; set; } = new();

    /// <summary>
    /// Total number of migrations in the batch.
    /// </summary>
    public int TotalCount => Results.Count;

    /// <summary>
    /// Number of successful migrations.
    /// </summary>
    public int SuccessCount => Results.Count(r => r.Success);

    /// <summary>
    /// Number of failed migrations.
    /// </summary>
    public int FailedCount => Results.Count(r => !r.Success);

    /// <summary>
    /// Number of skipped migrations (when StopOnFirstError mode is used).
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// When the batch execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the batch execution completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Total duration of the batch execution.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Whether the batch was executed as a dry run.
    /// </summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// Creates a successful batch result.
    /// </summary>
    public static BatchExecutionResult Succeeded(
        Guid batchJobId,
        Guid environmentId,
        List<ExecutionResult> results,
        DateTime startedAt)
    {
        return new BatchExecutionResult
        {
            BatchJobId = batchJobId,
            EnvironmentId = environmentId,
            Results = results,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            WasDryRun = results.FirstOrDefault()?.WasDryRun ?? false
        };
    }

    /// <summary>
    /// Creates a partially completed batch result (some failures).
    /// </summary>
    public static BatchExecutionResult Partial(
        Guid batchJobId,
        Guid environmentId,
        List<ExecutionResult> results,
        int skippedCount,
        DateTime startedAt)
    {
        return new BatchExecutionResult
        {
            BatchJobId = batchJobId,
            EnvironmentId = environmentId,
            Results = results,
            SkippedCount = skippedCount,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Event args for batch migration progress updates.
/// </summary>
public class BatchMigrationProgressEventArgs : EventArgs
{
    /// <summary>
    /// The batch job ID.
    /// </summary>
    public Guid BatchJobId { get; init; }

    /// <summary>
    /// Current migration being executed.
    /// </summary>
    public string CurrentMigrationId { get; init; } = string.Empty;

    /// <summary>
    /// Index of current migration (0-based).
    /// </summary>
    public int CurrentIndex { get; init; }

    /// <summary>
    /// Total number of migrations in the batch.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Overall progress percentage (0-100).
    /// </summary>
    public int OverallProgressPercentage => TotalCount > 0 ? (CurrentIndex * 100) / TotalCount : 0;

    /// <summary>
    /// Progress of current migration (0-100).
    /// </summary>
    public int CurrentMigrationProgress { get; init; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>
    /// Current phase of execution.
    /// </summary>
    public MigrationPhase Phase { get; init; }
}
