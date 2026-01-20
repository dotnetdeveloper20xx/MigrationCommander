namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents information about a database migration.
/// </summary>
public class MigrationInfo
{
    /// <summary>
    /// The unique identifier for the migration (e.g., "20240125000000_AddOrderTable").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable name of the migration (e.g., "AddOrderTable").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp parsed from the migration ID.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional description from XML comments or attributes.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The current status of this migration.
    /// </summary>
    public MigrationStatus Status { get; set; }

    /// <summary>
    /// When the migration was applied (if applicable).
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// User who applied the migration (if tracked).
    /// </summary>
    public string? AppliedBy { get; set; }

    /// <summary>
    /// The UP SQL for this migration (provider-specific).
    /// </summary>
    public string? UpSql { get; set; }

    /// <summary>
    /// The DOWN SQL for this migration (provider-specific).
    /// </summary>
    public string? DownSql { get; set; }

    /// <summary>
    /// List of tables affected by this migration.
    /// </summary>
    public List<string> AffectedTables { get; set; } = new();

    /// <summary>
    /// List of database objects created by this migration.
    /// </summary>
    public List<string> CreatedObjects { get; set; } = new();

    /// <summary>
    /// List of database objects dropped by this migration.
    /// </summary>
    public List<string> DroppedObjects { get; set; } = new();

    /// <summary>
    /// List of database objects modified by this migration.
    /// </summary>
    public List<string> ModifiedObjects { get; set; } = new();

    /// <summary>
    /// Indicates if this migration contains destructive operations (DROP, TRUNCATE, etc.).
    /// </summary>
    public bool IsDestructive { get; set; }

    /// <summary>
    /// Estimated duration based on historical data or analysis.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; set; }

    /// <summary>
    /// The full type name of the migration class.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// The assembly containing this migration.
    /// </summary>
    public string? AssemblyName { get; set; }
}
