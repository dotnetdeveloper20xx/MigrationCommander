using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Repositories;

/// <summary>
/// Repository for managing approval requests.
/// </summary>
public class ApprovalRequestRepository
{
    private readonly MigrationCommanderDbContext _context;

    public ApprovalRequestRepository(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalRequest> AddAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var entity = ApprovalRequestEntity.FromDomainModel(request);
        _context.ApprovalRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.ToDomainModel();
    }

    public async Task<ApprovalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity?.ToDomainModel();
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .Where(a => a.Status == nameof(ApprovalStatus.Pending))
            .OrderBy(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetByRequestedByAsync(string requestedBy, CancellationToken cancellationToken = default)
    {
        var entities = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .Where(a => a.RequestedBy == requestedBy)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .Where(a => a.EnvironmentId == environmentId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<ApprovalRequest?> GetByMigrationAndEnvironmentAsync(string migrationId, Guid environmentId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .Where(a => a.MigrationId == migrationId && a.EnvironmentId == environmentId)
            .OrderByDescending(a => a.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomainModel();
    }

    public async Task<ApprovalRequest?> GetApprovedAndValidAsync(string migrationId, Guid environmentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await _context.ApprovalRequests
            .Include(a => a.Environment)
            .Where(a => a.MigrationId == migrationId
                     && a.EnvironmentId == environmentId
                     && a.Status == nameof(ApprovalStatus.Approved)
                     && !a.IsUsed
                     && (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderByDescending(a => a.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity?.ToDomainModel();
    }

    public async Task<ApprovalRequest> UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApprovalRequests
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (entity == null)
            throw new InvalidOperationException($"Approval request with ID {request.Id} not found");

        entity.Status = request.Status.ToString();
        entity.ReviewedBy = request.ReviewedBy;
        entity.ReviewedByEmail = request.ReviewedByEmail;
        entity.ReviewedAt = request.ReviewedAt;
        entity.RequestComments = request.RequestComments;
        entity.ReviewComments = request.ReviewComments;
        entity.RejectionReason = request.RejectionReason;
        entity.IsUsed = request.IsUsed;
        entity.UsedAt = request.UsedAt;

        await _context.SaveChangesAsync(cancellationToken);
        return entity.ToDomainModel();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApprovalRequests.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        _context.ApprovalRequests.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAsUsedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ApprovalRequests.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            entity.IsUsed = true;
            entity.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ExpireOldRequestsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredRequests = await _context.ApprovalRequests
            .Where(a => a.Status == nameof(ApprovalStatus.Pending)
                     && a.ExpiresAt != null
                     && a.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var request in expiredRequests)
        {
            request.Status = nameof(ApprovalStatus.Expired);
        }

        if (expiredRequests.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalRequests
            .CountAsync(a => a.Status == nameof(ApprovalStatus.Pending), cancellationToken);
    }

    public async Task<int> GetPendingCountByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalRequests
            .CountAsync(a => a.EnvironmentId == environmentId
                          && a.Status == nameof(ApprovalStatus.Pending), cancellationToken);
    }
}
