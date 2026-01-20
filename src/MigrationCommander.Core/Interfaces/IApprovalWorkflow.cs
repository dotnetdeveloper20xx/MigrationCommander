using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for managing migration approval workflows.
/// </summary>
public interface IApprovalWorkflow
{
    /// <summary>
    /// Checks if approval is required for a migration on an environment.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="environmentId">The target environment ID.</param>
    /// <param name="isDestructive">Whether the migration is destructive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Approval requirement result.</returns>
    Task<ApprovalRequirement> CheckApprovalRequiredAsync(
        string migrationId,
        Guid environmentId,
        bool isDestructive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests approval for a migration.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="environmentId">The target environment ID.</param>
    /// <param name="requestedBy">User requesting approval.</param>
    /// <param name="requestedByEmail">Email of the requester.</param>
    /// <param name="comments">Optional comments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created approval request.</returns>
    Task<ApprovalRequest> RequestApprovalAsync(
        string migrationId,
        Guid environmentId,
        string requestedBy,
        string requestedByEmail,
        string? comments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a pending request.
    /// </summary>
    /// <param name="requestId">The approval request ID.</param>
    /// <param name="approvedBy">User approving.</param>
    /// <param name="approvedByEmail">Email of the approver.</param>
    /// <param name="comments">Optional comments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated approval request.</returns>
    Task<ApprovalRequest> ApproveAsync(
        Guid requestId,
        string approvedBy,
        string approvedByEmail,
        string? comments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a pending request.
    /// </summary>
    /// <param name="requestId">The approval request ID.</param>
    /// <param name="rejectedBy">User rejecting.</param>
    /// <param name="rejectedByEmail">Email of the rejector.</param>
    /// <param name="reason">Reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated approval request.</returns>
    Task<ApprovalRequest> RejectAsync(
        Guid requestId,
        string rejectedBy,
        string rejectedByEmail,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending request.
    /// </summary>
    /// <param name="requestId">The approval request ID.</param>
    /// <param name="cancelledBy">User cancelling.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancelled successfully.</returns>
    Task<bool> CancelAsync(
        Guid requestId,
        string cancelledBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific approval request.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approval request or null.</returns>
    Task<ApprovalRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending approval requests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending requests.</returns>
    Task<IReadOnlyList<ApprovalRequest>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approval requests for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's requests.</returns>
    Task<IReadOnlyList<ApprovalRequest>> GetByUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approval requests for a specific environment.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of requests for the environment.</returns>
    Task<IReadOnlyList<ApprovalRequest>> GetByEnvironmentAsync(
        Guid environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all approval requests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all requests.</returns>
    Task<IReadOnlyList<ApprovalRequest>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an approval as used.
    /// </summary>
    /// <param name="requestId">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsUsedAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an existing valid approval for a migration/environment.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Valid approval request or null.</returns>
    Task<ApprovalRequest?> GetValidApprovalAsync(
        string migrationId,
        Guid environmentId,
        CancellationToken cancellationToken = default);
}
