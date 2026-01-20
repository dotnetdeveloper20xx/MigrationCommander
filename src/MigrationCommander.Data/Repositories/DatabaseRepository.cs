using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Repositories;

/// <summary>
/// Repository for managing configured databases.
/// </summary>
public class DatabaseRepository
{
    private readonly MigrationCommanderDbContext _context;

    public DatabaseRepository(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ConfiguredDatabase>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguredDatabases
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConfiguredDatabase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguredDatabases
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<ConfiguredDatabase?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguredDatabases
            .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<ConfiguredDatabase>> GetByProviderAsync(ProviderType provider, CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguredDatabases
            .Where(d => d.Provider == provider && d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConfiguredDatabase> AddAsync(ConfiguredDatabase database, CancellationToken cancellationToken = default)
    {
        database.CreatedAt = DateTime.UtcNow;
        _context.ConfiguredDatabases.Add(database);
        await _context.SaveChangesAsync(cancellationToken);
        return database;
    }

    public async Task UpdateAsync(ConfiguredDatabase database, CancellationToken cancellationToken = default)
    {
        database.UpdatedAt = DateTime.UtcNow;
        _context.ConfiguredDatabases.Update(database);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var database = await GetByIdAsync(id, cancellationToken);
        if (database != null)
        {
            _context.ConfiguredDatabases.Remove(database);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.ConfiguredDatabases
            .AnyAsync(d => d.Name == name, cancellationToken);
    }
}
