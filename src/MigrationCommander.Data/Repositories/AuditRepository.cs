using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Repositories;

/// <summary>
/// Repository for managing audit logs.
/// </summary>
public class AuditRepository
{
    private readonly MigrationCommanderDbContext _context;

    public AuditRepository(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> AddAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);

        query = ApplySorting(query, filter);

        return await query
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        return await query.CountAsync(cancellationToken);
    }

    private IQueryable<AuditLog> BuildQuery(AuditLogFilter filter)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.ToDate.Value);
        }

        if (filter.EnvironmentId.HasValue)
        {
            query = query.Where(a => a.EnvironmentId == filter.EnvironmentId.Value);
        }

        if (filter.Provider.HasValue)
        {
            query = query.Where(a => a.Provider == filter.Provider.Value);
        }

        if (filter.Action.HasValue)
        {
            query = query.Where(a => a.Action == filter.Action.Value);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            query = query.Where(a => a.UserId == filter.UserId);
        }

        if (!string.IsNullOrEmpty(filter.MigrationId))
        {
            query = query.Where(a => a.MigrationId == filter.MigrationId);
        }

        if (filter.SuccessOnly.HasValue)
        {
            query = query.Where(a => a.Success == filter.SuccessOnly.Value);
        }

        return query;
    }

    private static IQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> query, AuditLogFilter filter)
    {
        return filter.SortBy.ToLowerInvariant() switch
        {
            "timestamp" => filter.SortDescending
                ? query.OrderByDescending(a => a.Timestamp)
                : query.OrderBy(a => a.Timestamp),
            "action" => filter.SortDescending
                ? query.OrderByDescending(a => a.Action)
                : query.OrderBy(a => a.Action),
            "userid" => filter.SortDescending
                ? query.OrderByDescending(a => a.UserId)
                : query.OrderBy(a => a.UserId),
            "success" => filter.SortDescending
                ? query.OrderByDescending(a => a.Success)
                : query.OrderBy(a => a.Success),
            _ => query.OrderByDescending(a => a.Timestamp)
        };
    }
}
