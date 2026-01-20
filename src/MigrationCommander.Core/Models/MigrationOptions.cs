namespace MigrationCommander.Core.Models;

/// <summary>
/// Options for migration execution.
/// </summary>
public class MigrationOptions
{
    /// <summary>
    /// If true, generates and returns SQL without executing.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// If true, creates a backup before applying the migration.
    /// </summary>
    public bool CreateBackup { get; set; } = false;

    /// <summary>
    /// Maximum time allowed for the migration to complete.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// If true, stops execution on first error when applying multiple migrations.
    /// </summary>
    public bool StopOnError { get; set; } = true;

    /// <summary>
    /// Optional reason or notes for the migration.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// If true, skips confirmation prompts (use with caution).
    /// </summary>
    public bool SkipConfirmation { get; set; } = false;
}
