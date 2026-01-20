namespace MigrationCommander.Core.Models;

/// <summary>
/// Filter criteria for generating reports.
/// </summary>
public class ReportFilter
{
    /// <summary>
    /// Start date for the report period.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for the report period.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by specific environment(s).
    /// </summary>
    public List<Guid>? EnvironmentIds { get; set; }

    /// <summary>
    /// Filter by provider type.
    /// </summary>
    public ProviderType? Provider { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by migration status.
    /// </summary>
    public MigrationStatus? Status { get; set; }

    /// <summary>
    /// Include only successful executions.
    /// </summary>
    public bool? SuccessOnly { get; set; }

    /// <summary>
    /// Include destructive migrations only.
    /// </summary>
    public bool? DestructiveOnly { get; set; }
}

/// <summary>
/// Available report formats.
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// PDF document.
    /// </summary>
    Pdf,

    /// <summary>
    /// Excel spreadsheet.
    /// </summary>
    Excel,

    /// <summary>
    /// CSV file.
    /// </summary>
    Csv,

    /// <summary>
    /// JSON data.
    /// </summary>
    Json,

    /// <summary>
    /// HTML document.
    /// </summary>
    Html
}

/// <summary>
/// Types of reports that can be generated.
/// </summary>
public enum ReportType
{
    /// <summary>
    /// Migration execution summary report.
    /// </summary>
    MigrationSummary,

    /// <summary>
    /// Detailed audit trail report.
    /// </summary>
    AuditTrail,

    /// <summary>
    /// Environment status report.
    /// </summary>
    EnvironmentStatus,

    /// <summary>
    /// Environment comparison report.
    /// </summary>
    EnvironmentComparison,

    /// <summary>
    /// User activity report.
    /// </summary>
    UserActivity
}
