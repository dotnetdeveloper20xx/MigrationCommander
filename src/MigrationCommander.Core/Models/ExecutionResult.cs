namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents the result of a migration execution.
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Indicates if the migration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The ID of the migration that was executed.
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// The environment where the migration was executed.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// The database provider used.
    /// </summary>
    public ProviderType Provider { get; set; }

    /// <summary>
    /// When the migration execution started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the migration execution completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// The total duration of the migration execution.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// The SQL that was executed.
    /// </summary>
    public string? ExecutedSql { get; set; }

    /// <summary>
    /// Error message if the migration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if an exception occurred.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Number of rows affected by the migration.
    /// </summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// Any warnings generated during execution.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Indicates if the migration was a dry run (no actual changes).
    /// </summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    public static ExecutionResult Succeeded(string migrationId, Guid environmentId, ProviderType provider, DateTime startedAt)
    {
        return new ExecutionResult
        {
            Success = true,
            MigrationId = migrationId,
            EnvironmentId = environmentId,
            Provider = provider,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed execution result.
    /// </summary>
    public static ExecutionResult Failed(string migrationId, Guid environmentId, ProviderType provider, DateTime startedAt, string errorMessage, string? stackTrace = null)
    {
        return new ExecutionResult
        {
            Success = false,
            MigrationId = migrationId,
            EnvironmentId = environmentId,
            Provider = provider,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        };
    }
}
