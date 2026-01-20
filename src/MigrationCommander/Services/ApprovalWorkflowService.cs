using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for managing migration approval workflows.
/// Uses database storage via ApprovalRequestRepository.
/// </summary>
public class ApprovalWorkflowService : IApprovalWorkflow
{
    private readonly DatabaseRepository _databaseRepository;
    private readonly ApprovalRequestRepository _approvalRequestRepository;
    private readonly IAuditLogger _auditLogger;

    // Approval rules are kept in-memory as configuration
    private readonly Dictionary<Guid, ApprovalRule> _rules = new();
    private readonly object _ruleLock = new();

    public ApprovalWorkflowService(
        DatabaseRepository databaseRepository,
        ApprovalRequestRepository approvalRequestRepository,
        IAuditLogger auditLogger)
    {
        _databaseRepository = databaseRepository;
        _approvalRequestRepository = approvalRequestRepository;
        _auditLogger = auditLogger;

        // Initialize with default rules
        InitializeDefaultRules();
    }

    private void InitializeDefaultRules()
    {
        // Default rule: Require approval for production environments
        var productionRule = new ApprovalRule
        {
            Id = Guid.NewGuid(),
            Name = "Production Approval",
            Description = "Requires approval for all migrations to production environments",
            EnvironmentId = null, // Applies to production environments
            IsActive = true,
            RequiredApprovals = 1,
            ApproverRoles = new List<string> { "dba", "admin" },
            RequiresDifferentApprover = true,
            RequiredForDestructive = true,
            RequiredForAll = false,
            ExpirationHours = 24,
            CreatedAt = DateTime.UtcNow
        };

        _rules[productionRule.Id] = productionRule;
    }

    /// <inheritdoc />
    public async Task<ApprovalRequirement> CheckApprovalRequiredAsync(
        string migrationId,
        Guid environmentId,
        bool isDestructive,
        CancellationToken cancellationToken = default)
    {
        var environment = await _databaseRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            return ApprovalRequirement.NotRequired();
        }

        // Check if there's already a valid approval
        var existingApproval = await GetValidApprovalAsync(migrationId, environmentId, cancellationToken);
        if (existingApproval != null)
        {
            return new ApprovalRequirement
            {
                IsRequired = false,
                ExistingApproval = existingApproval
            };
        }

        // Find applicable rules
        ApprovalRule? applicableRule = null;

        lock (_ruleLock)
        {
            foreach (var rule in _rules.Values.Where(r => r.IsActive))
            {
                // Check if rule applies to this environment
                if (rule.EnvironmentId.HasValue && rule.EnvironmentId != environmentId)
                {
                    continue;
                }

                // Check if this is a production environment
                if (!rule.EnvironmentId.HasValue && !environment.IsProduction)
                {
                    continue;
                }

                // Check if approval is required based on destructive flag
                if (isDestructive && rule.RequiredForDestructive)
                {
                    applicableRule = rule;
                    break;
                }

                // Check if approval is required for all migrations
                if (rule.RequiredForAll)
                {
                    applicableRule = rule;
                    break;
                }
            }
        }

        if (applicableRule == null)
        {
            return ApprovalRequirement.NotRequired();
        }

        var reason = isDestructive
            ? $"Destructive migration requires approval for {environment.DisplayName}"
            : $"Approval required by rule '{applicableRule.Name}'";

        return ApprovalRequirement.Required(applicableRule, reason);
    }

    /// <inheritdoc />
    public async Task<ApprovalRequest> RequestApprovalAsync(
        string migrationId,
        Guid environmentId,
        string requestedBy,
        string requestedByEmail,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID is required", nameof(migrationId));

        if (string.IsNullOrWhiteSpace(requestedBy))
            throw new ArgumentException("Requester is required", nameof(requestedBy));

        var environment = await _databaseRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            throw new ArgumentException($"Environment not found: {environmentId}", nameof(environmentId));
        }

        // Check for existing pending request
        var existingRequests = await _approvalRequestRepository.GetByEnvironmentAsync(environmentId, cancellationToken);
        var existingPending = existingRequests.FirstOrDefault(r =>
            r.MigrationId == migrationId && r.Status == ApprovalStatus.Pending);

        if (existingPending != null)
        {
            throw new InvalidOperationException(
                $"A pending approval request already exists for migration '{migrationId}' on environment '{environment.DisplayName}'");
        }

        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            MigrationId = migrationId,
            EnvironmentId = environmentId,
            EnvironmentName = environment.DisplayName,
            RequestedBy = requestedBy,
            RequestedByEmail = requestedByEmail,
            RequestedAt = DateTime.UtcNow,
            Status = ApprovalStatus.Pending,
            RequestComments = comments,
            ExpiresAt = DateTime.UtcNow.AddHours(24) // Default 24 hour expiration
        };

        var createdRequest = await _approvalRequestRepository.AddAsync(request, cancellationToken);

        // Log the request
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Action = AuditAction.ApprovalRequested,
            UserId = requestedBy,
            UserEmail = requestedByEmail,
            UserIpAddress = "system",
            MigrationId = migrationId,
            EnvironmentId = environmentId,
            EnvironmentName = environment.DisplayName,
            Provider = environment.Provider,
            Success = true,
            Duration = TimeSpan.Zero,
            Notes = comments
        }, cancellationToken);

        return createdRequest;
    }

    /// <inheritdoc />
    public async Task<ApprovalRequest> ApproveAsync(
        Guid requestId,
        string approvedBy,
        string approvedByEmail,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("Approver is required", nameof(approvedBy));

        var request = await _approvalRequestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Approval request not found: {requestId}");
        }

        if (request.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Request is not pending. Current status: {request.Status}");
        }

        // Check if approver is different from requester
        if (request.RequestedBy.Equals(approvedBy, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot approve your own request");
        }

        // Check expiration
        if (request.ExpiresAt.HasValue && request.ExpiresAt < DateTime.UtcNow)
        {
            request.Status = ApprovalStatus.Expired;
            await _approvalRequestRepository.UpdateAsync(request, cancellationToken);
            throw new InvalidOperationException("Request has expired");
        }

        request.Status = ApprovalStatus.Approved;
        request.ReviewedBy = approvedBy;
        request.ReviewedByEmail = approvedByEmail;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewComments = comments;

        var updatedRequest = await _approvalRequestRepository.UpdateAsync(request, cancellationToken);

        // Log the approval
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Action = AuditAction.ApprovalGranted,
            UserId = approvedBy,
            UserEmail = approvedByEmail,
            UserIpAddress = "system",
            MigrationId = request.MigrationId,
            EnvironmentId = request.EnvironmentId,
            EnvironmentName = request.EnvironmentName,
            Success = true,
            Duration = TimeSpan.Zero,
            Notes = comments
        }, cancellationToken);

        return updatedRequest;
    }

    /// <inheritdoc />
    public async Task<ApprovalRequest> RejectAsync(
        Guid requestId,
        string rejectedBy,
        string rejectedByEmail,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rejectedBy))
            throw new ArgumentException("Rejector is required", nameof(rejectedBy));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required", nameof(reason));

        var request = await _approvalRequestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Approval request not found: {requestId}");
        }

        if (request.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Request is not pending. Current status: {request.Status}");
        }

        request.Status = ApprovalStatus.Rejected;
        request.ReviewedBy = rejectedBy;
        request.ReviewedByEmail = rejectedByEmail;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = reason;

        var updatedRequest = await _approvalRequestRepository.UpdateAsync(request, cancellationToken);

        // Log the rejection
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Action = AuditAction.ApprovalDenied,
            UserId = rejectedBy,
            UserEmail = rejectedByEmail,
            UserIpAddress = "system",
            MigrationId = request.MigrationId,
            EnvironmentId = request.EnvironmentId,
            EnvironmentName = request.EnvironmentName,
            Success = true,
            Duration = TimeSpan.Zero,
            Notes = reason
        }, cancellationToken);

        return updatedRequest;
    }

    /// <inheritdoc />
    public async Task<bool> CancelAsync(
        Guid requestId,
        string cancelledBy,
        CancellationToken cancellationToken = default)
    {
        var request = await _approvalRequestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            return false;
        }

        if (request.Status != ApprovalStatus.Pending)
        {
            return false;
        }

        // Only the requester can cancel
        if (!request.RequestedBy.Equals(cancelledBy, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        request.Status = ApprovalStatus.Cancelled;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewComments = "Cancelled by requester";

        await _approvalRequestRepository.UpdateAsync(request, cancellationToken);

        // Log the cancellation
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Action = AuditAction.ApprovalCancelled,
            UserId = cancelledBy,
            UserEmail = request.RequestedByEmail,
            UserIpAddress = "system",
            MigrationId = request.MigrationId,
            EnvironmentId = request.EnvironmentId,
            EnvironmentName = request.EnvironmentName,
            Success = true,
            Duration = TimeSpan.Zero,
            Notes = "Cancelled by requester"
        }, cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<ApprovalRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return await _approvalRequestRepository.GetByIdAsync(requestId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        // Expire old requests first
        await _approvalRequestRepository.ExpireOldRequestsAsync(cancellationToken);

        return await _approvalRequestRepository.GetPendingAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRequest>> GetByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _approvalRequestRepository.GetByRequestedByAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRequest>> GetByEnvironmentAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        return await _approvalRequestRepository.GetByEnvironmentAsync(environmentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalRequest>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _approvalRequestRepository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsUsedAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await _approvalRequestRepository.MarkAsUsedAsync(requestId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApprovalRequest?> GetValidApprovalAsync(
        string migrationId,
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        return await _approvalRequestRepository.GetApprovedAndValidAsync(migrationId, environmentId, cancellationToken);
    }
}
