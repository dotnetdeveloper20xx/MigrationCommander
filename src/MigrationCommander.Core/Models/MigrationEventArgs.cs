namespace MigrationCommander.Core.Models;

/// <summary>
/// Event arguments for when a migration is about to be executed.
/// </summary>
public class MigrationExecutingEventArgs : EventArgs
{
    /// <summary>
    /// The migration being executed.
    /// </summary>
    public MigrationInfo Migration { get; }

    /// <summary>
    /// The target environment.
    /// </summary>
    public DatabaseEnvironment Environment { get; }

    /// <summary>
    /// The SQL that will be executed.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// Set to true to cancel the migration.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Reason for cancellation (if Cancel is true).
    /// </summary>
    public string? CancelReason { get; set; }

    public MigrationExecutingEventArgs(MigrationInfo migration, DatabaseEnvironment environment, string sql)
    {
        Migration = migration;
        Environment = environment;
        Sql = sql;
    }
}

/// <summary>
/// Event arguments for when a migration has been executed.
/// </summary>
public class MigrationExecutedEventArgs : EventArgs
{
    /// <summary>
    /// The migration that was executed.
    /// </summary>
    public MigrationInfo Migration { get; }

    /// <summary>
    /// The target environment.
    /// </summary>
    public DatabaseEnvironment Environment { get; }

    /// <summary>
    /// The result of the execution.
    /// </summary>
    public ExecutionResult Result { get; }

    public MigrationExecutedEventArgs(MigrationInfo migration, DatabaseEnvironment environment, ExecutionResult result)
    {
        Migration = migration;
        Environment = environment;
        Result = result;
    }
}

/// <summary>
/// Event arguments for migration progress updates.
/// </summary>
public class MigrationProgressEventArgs : EventArgs
{
    /// <summary>
    /// The migration being executed.
    /// </summary>
    public string MigrationId { get; }

    /// <summary>
    /// Current progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; }

    /// <summary>
    /// Current status message.
    /// </summary>
    public string StatusMessage { get; }

    /// <summary>
    /// Current phase of the migration.
    /// </summary>
    public MigrationPhase Phase { get; }

    public MigrationProgressEventArgs(string migrationId, int progressPercentage, string statusMessage, MigrationPhase phase)
    {
        MigrationId = migrationId;
        ProgressPercentage = progressPercentage;
        StatusMessage = statusMessage;
        Phase = phase;
    }
}

/// <summary>
/// Phases of migration execution.
/// </summary>
public enum MigrationPhase
{
    Preparing,
    Validating,
    Executing,
    Verifying,
    Completed,
    Failed,
    RollingBack
}
