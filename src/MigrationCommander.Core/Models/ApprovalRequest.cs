namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents a request for approval to apply a migration.
/// </summary>
public class ApprovalRequest
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The migration ID requesting approval.
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// Target environment ID.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// Display name of the target environment.
    /// </summary>
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>
    /// User who requested the approval.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>
    /// Email of the requester.
    /// </summary>
    public string RequestedByEmail { get; set; } = string.Empty;

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// Current status of the request.
    /// </summary>
    public ApprovalStatus Status { get; set; }

    /// <summary>
    /// User who approved/rejected the request.
    /// </summary>
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// Email of the reviewer.
    /// </summary>
    public string? ReviewedByEmail { get; set; }

    /// <summary>
    /// When the request was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Comments from the requester.
    /// </summary>
    public string? RequestComments { get; set; }

    /// <summary>
    /// Comments from the reviewer.
    /// </summary>
    public string? ReviewComments { get; set; }

    /// <summary>
    /// Reason for rejection (if rejected).
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// When the approval expires (if not acted upon).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the approval has been used to apply the migration.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// When the approval was used.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Whether the request is pending.
    /// </summary>
    public bool IsPending => Status == ApprovalStatus.Pending;

    /// <summary>
    /// Whether the request is approved and not expired or used.
    /// </summary>
    public bool IsApprovedAndValid => Status == ApprovalStatus.Approved
        && !IsUsed
        && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}

/// <summary>
/// Status of an approval request.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Waiting for approval.
    /// </summary>
    Pending,

    /// <summary>
    /// Approved by reviewer.
    /// </summary>
    Approved,

    /// <summary>
    /// Rejected by reviewer.
    /// </summary>
    Rejected,

    /// <summary>
    /// Expired without action.
    /// </summary>
    Expired,

    /// <summary>
    /// Cancelled by requester.
    /// </summary>
    Cancelled
}
