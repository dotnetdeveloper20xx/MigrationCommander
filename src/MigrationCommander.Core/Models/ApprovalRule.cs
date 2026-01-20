namespace MigrationCommander.Core.Models;

/// <summary>
/// Defines rules for when approval is required.
/// </summary>
public class ApprovalRule
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Environment ID this rule applies to (null = all environments).
    /// </summary>
    public Guid? EnvironmentId { get; set; }

    /// <summary>
    /// Name of the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the rule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of approvals required.
    /// </summary>
    public int RequiredApprovals { get; set; } = 1;

    /// <summary>
    /// Role names that can approve (empty = any user with ApproveProduction permission).
    /// </summary>
    public List<string> ApproverRoles { get; set; } = new();

    /// <summary>
    /// Whether the approver must be different from the requester.
    /// </summary>
    public bool RequiresDifferentApprover { get; set; } = true;

    /// <summary>
    /// Whether approval is required for destructive migrations.
    /// </summary>
    public bool RequiredForDestructive { get; set; } = true;

    /// <summary>
    /// Whether approval is required for all migrations (not just destructive).
    /// </summary>
    public bool RequiredForAll { get; set; } = false;

    /// <summary>
    /// Hours until approval expires (0 = never expires).
    /// </summary>
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// When the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the rule was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Result of checking if approval is required.
/// </summary>
public class ApprovalRequirement
{
    /// <summary>
    /// Whether approval is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// The rule that requires approval.
    /// </summary>
    public ApprovalRule? Rule { get; set; }

    /// <summary>
    /// Reason why approval is required.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Existing approval request (if any).
    /// </summary>
    public ApprovalRequest? ExistingApproval { get; set; }

    /// <summary>
    /// Whether the existing approval is valid.
    /// </summary>
    public bool HasValidApproval => ExistingApproval?.IsApprovedAndValid ?? false;

    /// <summary>
    /// Creates a result indicating no approval is required.
    /// </summary>
    public static ApprovalRequirement NotRequired() => new() { IsRequired = false };

    /// <summary>
    /// Creates a result indicating approval is required.
    /// </summary>
    public static ApprovalRequirement Required(ApprovalRule rule, string reason) => new()
    {
        IsRequired = true,
        Rule = rule,
        Reason = reason
    };
}
