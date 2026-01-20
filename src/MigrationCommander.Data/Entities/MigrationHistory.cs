using MigrationCommander.Core.Models;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing the history of a migration execution.
/// </summary>
public class MigrationHistory
{
    public Guid Id { get; set; }
    public string MigrationId { get; set; } = string.Empty;
    public Guid EnvironmentId { get; set; }
    public ProviderType Provider { get; set; }

    public MigrationStatus Status { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? ExecutedBy { get; set; }

    public TimeSpan Duration { get; set; }
    public int RowsAffected { get; set; }

    /// <summary>
    /// The SQL that was executed (stored for audit purposes).
    /// </summary>
    public string? ExecutedSql { get; set; }

    /// <summary>
    /// Error message if the migration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if an error occurred.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// User notes for this migration execution.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Reference to the configured database.
    /// </summary>
    public ConfiguredDatabase? Environment { get; set; }
}
