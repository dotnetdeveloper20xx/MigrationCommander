namespace MigrationCommander.Core.Models;

/// <summary>
/// Options for rollback operations.
/// </summary>
public class RollbackOptions
{
    /// <summary>
    /// If true, forces the rollback even with warnings.
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// If true, creates a backup before rolling back.
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// Required reason for the rollback (for audit purposes).
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Maximum time allowed for the rollback to complete.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// If true, skips confirmation prompts (use with extreme caution).
    /// </summary>
    public bool SkipConfirmation { get; set; } = false;
}
