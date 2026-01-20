namespace MigrationCommander.Core.Models;

/// <summary>
/// Filter criteria for querying audit logs.
/// </summary>
public class AuditLogFilter
{
    /// <summary>
    /// Filter by entries after this date.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by entries before this date.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by specific environment.
    /// </summary>
    public Guid? EnvironmentId { get; set; }

    /// <summary>
    /// Filter by database provider.
    /// </summary>
    public ProviderType? Provider { get; set; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by migration ID.
    /// </summary>
    public string? MigrationId { get; set; }

    /// <summary>
    /// If true, only return successful operations.
    /// If false, only return failed operations.
    /// If null, return all.
    /// </summary>
    public bool? SuccessOnly { get; set; }

    /// <summary>
    /// Number of entries to skip (for pagination).
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of entries to take (for pagination).
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    /// Sort by column name.
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort in descending order.
    /// </summary>
    public bool SortDescending { get; set; } = true;
}
