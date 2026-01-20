namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents the current status of a migration.
/// </summary>
public enum MigrationStatus
{
    /// <summary>
    /// Migration has not been applied yet.
    /// </summary>
    Pending,

    /// <summary>
    /// Migration has been successfully applied.
    /// </summary>
    Applied,

    /// <summary>
    /// Migration execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Migration was applied but has been rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Migration was skipped during execution.
    /// </summary>
    Skipped
}
