using MigrationCommander.Core.Models;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing an approval request for a migration.
/// </summary>
public class ApprovalRequestEntity
{
    public Guid Id { get; set; }
    public string MigrationId { get; set; } = string.Empty;
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedByEmail { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = nameof(ApprovalStatus.Pending);
    public string? ReviewedBy { get; set; }
    public string? ReviewedByEmail { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RequestComments { get; set; }
    public string? ReviewComments { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Navigation property to the environment.
    /// </summary>
    public ConfiguredDatabase? Environment { get; set; }

    /// <summary>
    /// Converts to the domain model.
    /// </summary>
    public ApprovalRequest ToDomainModel()
    {
        Enum.TryParse<ApprovalStatus>(Status, out var status);

        return new ApprovalRequest
        {
            Id = Id,
            MigrationId = MigrationId,
            EnvironmentId = EnvironmentId,
            EnvironmentName = EnvironmentName,
            RequestedBy = RequestedBy,
            RequestedByEmail = RequestedByEmail,
            RequestedAt = RequestedAt,
            Status = status,
            ReviewedBy = ReviewedBy,
            ReviewedByEmail = ReviewedByEmail,
            ReviewedAt = ReviewedAt,
            RequestComments = RequestComments,
            ReviewComments = ReviewComments,
            RejectionReason = RejectionReason,
            ExpiresAt = ExpiresAt,
            IsUsed = IsUsed,
            UsedAt = UsedAt
        };
    }

    /// <summary>
    /// Creates from a domain model.
    /// </summary>
    public static ApprovalRequestEntity FromDomainModel(ApprovalRequest request)
    {
        return new ApprovalRequestEntity
        {
            Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
            MigrationId = request.MigrationId,
            EnvironmentId = request.EnvironmentId,
            EnvironmentName = request.EnvironmentName,
            RequestedBy = request.RequestedBy,
            RequestedByEmail = request.RequestedByEmail,
            RequestedAt = request.RequestedAt == default ? DateTime.UtcNow : request.RequestedAt,
            Status = request.Status.ToString(),
            ReviewedBy = request.ReviewedBy,
            ReviewedByEmail = request.ReviewedByEmail,
            ReviewedAt = request.ReviewedAt,
            RequestComments = request.RequestComments,
            ReviewComments = request.ReviewComments,
            RejectionReason = request.RejectionReason,
            ExpiresAt = request.ExpiresAt,
            IsUsed = request.IsUsed,
            UsedAt = request.UsedAt
        };
    }
}
