namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents an entry in the audit log.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Unique identifier for the audit entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID who performed the action.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email of the user who performed the action.
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the user.
    /// </summary>
    public string UserIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The type of action performed.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// The migration ID involved (if applicable).
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// The environment ID involved.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// The environment name for display purposes.
    /// </summary>
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>
    /// The database provider involved.
    /// </summary>
    public ProviderType Provider { get; set; }

    /// <summary>
    /// Indicates if the action was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// The SQL that was executed (if applicable).
    /// </summary>
    public string? ExecutedSql { get; set; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional notes or context.
    /// </summary>
    public string? Notes { get; set; }
}
