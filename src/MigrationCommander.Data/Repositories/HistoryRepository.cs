using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Repositories;

/// <summary>
/// Repository for managing migration history.
/// </summary>
public class HistoryRepository
{
    private readonly MigrationCommanderDbContext _context;

    public HistoryRepository(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    public async Task<MigrationHistory> AddAsync(MigrationHistory history, CancellationToken cancellationToken = default)
    {
        _context.MigrationHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);
        return history;
    }

    public async Task<MigrationHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MigrationHistories
            .Include(h => h.Environment)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationHistory>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        return await _context.MigrationHistories
            .Where(h => h.EnvironmentId == environmentId)
            .OrderByDescending(h => h.ExecutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationHistory>> GetByMigrationIdAsync(string migrationId, CancellationToken cancellationToken = default)
    {
        return await _context.MigrationHistories
            .Include(h => h.Environment)
            .Where(h => h.MigrationId == migrationId)
            .OrderByDescending(h => h.ExecutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MigrationHistory?> GetLatestAsync(Guid environmentId, string migrationId, CancellationToken cancellationToken = default)
    {
        return await _context.MigrationHistories
            .Where(h => h.EnvironmentId == environmentId && h.MigrationId == migrationId)
            .OrderByDescending(h => h.ExecutedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationHistory>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.MigrationHistories
            .Include(h => h.Environment)
            .OrderByDescending(h => h.ExecutedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationHistory>> GetFailedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MigrationHistories
            .Include(h => h.Environment)
            .Where(h => h.Status == MigrationStatus.Failed)
            .OrderByDescending(h => h.ExecutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, MigrationStatus>> GetStatusByEnvironmentAsync(string migrationId, CancellationToken cancellationToken = default)
    {
        var histories = await _context.MigrationHistories
            .Where(h => h.MigrationId == migrationId)
            .GroupBy(h => h.EnvironmentId)
            .Select(g => new
            {
                EnvironmentId = g.Key,
                LatestHistory = g.OrderByDescending(h => h.ExecutedAt).First()
            })
            .ToListAsync(cancellationToken);

        return histories.ToDictionary(
            h => h.EnvironmentId,
            h => h.LatestHistory.Status);
    }
}
