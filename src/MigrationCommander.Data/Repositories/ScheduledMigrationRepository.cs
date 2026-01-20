using Microsoft.EntityFrameworkCore;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Repositories;

/// <summary>
/// Repository for managing scheduled migrations.
/// </summary>
public class ScheduledMigrationRepository
{
    private readonly MigrationCommanderDbContext _context;

    public ScheduledMigrationRepository(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    public async Task<ScheduledMigration> AddAsync(ScheduledMigration scheduled, CancellationToken cancellationToken = default)
    {
        scheduled.CreatedAt = DateTime.UtcNow;
        _context.ScheduledMigrations.Add(scheduled);
        await _context.SaveChangesAsync(cancellationToken);
        return scheduled;
    }

    public async Task<ScheduledMigration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledMigrations
            .Include(s => s.Environment)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledMigration>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledMigrations
            .Include(s => s.Environment)
            .Where(s => s.Status == ScheduledMigrationStatus.Pending)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledMigration>> GetDueAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledMigrations
            .Include(s => s.Environment)
            .Where(s => s.Status == ScheduledMigrationStatus.Pending && s.ScheduledAt <= asOf)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledMigration>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ScheduledMigrations
            .Where(s => s.EnvironmentId == environmentId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScheduledMigration scheduled, CancellationToken cancellationToken = default)
    {
        _context.ScheduledMigrations.Update(scheduled);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid id, string cancelledBy, string? reason, CancellationToken cancellationToken = default)
    {
        var scheduled = await GetByIdAsync(id, cancellationToken);
        if (scheduled != null && scheduled.Status == ScheduledMigrationStatus.Pending)
        {
            scheduled.Status = ScheduledMigrationStatus.Cancelled;
            scheduled.CancelledBy = cancelledBy;
            scheduled.CancelledAt = DateTime.UtcNow;
            scheduled.CancellationReason = reason;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsRunningAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scheduled = await GetByIdAsync(id, cancellationToken);
        if (scheduled != null)
        {
            scheduled.Status = ScheduledMigrationStatus.Running;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsCompletedAsync(Guid id, bool success, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var scheduled = await GetByIdAsync(id, cancellationToken);
        if (scheduled != null)
        {
            scheduled.Status = success ? ScheduledMigrationStatus.Completed : ScheduledMigrationStatus.Failed;
            scheduled.ExecutedAt = DateTime.UtcNow;
            scheduled.ExecutionSuccess = success;
            scheduled.ErrorMessage = errorMessage;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
