using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models.Security;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Repositories;

/// <summary>
/// Repository for managing users and roles.
/// </summary>
public class UserRepository
{
    private readonly MigrationCommanderDbContext _context;

    public UserRepository(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    #region User Operations

    public async Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var entity = UserEntity.FromDomainModel(user);
        _context.Users.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.ToDomainModel();
    }

    public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (entity == null) return null;

        var user = entity.ToDomainModel();
        PopulateUserRoles(user, entity);
        return user;
    }

    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (entity == null) return null;

        var user = entity.ToDomainModel();
        PopulateUserRoles(user, entity);
        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (entity == null) return null;

        var user = entity.ToDomainModel();
        PopulateUserRoles(user, entity);
        return user;
    }

    public async Task<User?> GetUserByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

        if (entity == null) return null;

        var user = entity.ToDomainModel();
        PopulateUserRoles(user, entity);
        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return entities.Select(e =>
        {
            var user = e.ToDomainModel();
            PopulateUserRoles(user, e);
            return user;
        }).ToList();
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return entities.Select(e =>
        {
            var user = e.ToDomainModel();
            PopulateUserRoles(user, e);
            return user;
        }).ToList();
    }

    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);

        if (entity == null)
            throw new InvalidOperationException($"User with ID {user.Id} not found");

        entity.Username = user.Username;
        entity.Email = user.Email;
        entity.DisplayName = user.DisplayName;
        entity.IsActive = user.IsActive;
        entity.ExternalId = user.ExternalId;
        entity.Notes = user.Notes;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.LastLoginAt = user.LastLoginAt;

        await _context.SaveChangesAsync(cancellationToken);
        return entity.ToDomainModel();
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        _context.Users.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (entity != null)
        {
            entity.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region Role Operations

    public async Task<Role> AddRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        var entity = RoleEntity.FromDomainModel(role);
        _context.Roles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.ToDomainModel();
    }

    public async Task<Role?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
        return entity?.ToDomainModel();
    }

    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        return entity?.ToDomainModel();
    }

    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    public async Task<Role> UpdateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == role.Id, cancellationToken);

        if (entity == null)
            throw new InvalidOperationException($"Role with ID {role.Id} not found");

        entity.Name = role.Name;
        entity.DisplayName = role.DisplayName;
        entity.Description = role.Description;
        entity.PermissionsJson = string.Join(",", role.Permissions.Select(p => p.ToString()));
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return entity.ToDomainModel();
    }

    public async Task<bool> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        if (entity.IsBuiltIn)
            throw new InvalidOperationException("Cannot delete built-in roles");

        _context.Roles.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region User-Role Operations

    public async Task AssignRoleAsync(Guid userId, Guid roleId, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (existing != null) return; // Already assigned

        var userRole = new UserRoleEntity
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleEntities = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .Where(r => r != null)
            .ToListAsync(cancellationToken);

        return roleEntities.Select(r => r!.ToDomainModel()).ToList();
    }

    public async Task<IReadOnlyList<User>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var userEntities = await _context.UserRoles
            .Include(ur => ur.User)
                .ThenInclude(u => u!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Where(ur => ur.RoleId == roleId && ur.User != null)
            .Select(ur => ur.User!)
            .ToListAsync(cancellationToken);

        return userEntities.Select(e =>
        {
            var user = e.ToDomainModel();
            PopulateUserRoles(user, e);
            return user;
        }).ToList();
    }

    public async Task SetUserRolesAsync(Guid userId, IEnumerable<Guid> roleIds, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        // Remove existing roles
        var existingRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.UserRoles.RemoveRange(existingRoles);

        // Add new roles
        var roleIdList = roleIds.ToList();
        foreach (var roleId in roleIdList)
        {
            _context.UserRoles.Add(new UserRoleEntity
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Seeding

    public async Task SeedDefaultRolesAsync(CancellationToken cancellationToken = default)
    {
        var existingRoles = await _context.Roles.AnyAsync(cancellationToken);
        if (existingRoles) return;

        var defaultRoles = GetDefaultRoles();
        foreach (var role in defaultRoles)
        {
            var entity = RoleEntity.FromDomainModel(role);
            entity.IsBuiltIn = true;
            _context.Roles.Add(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static List<Role> GetDefaultRoles()
    {
        return new List<Role>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                DisplayName = "Administrator",
                Description = "Full system access",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                Permissions = Enum.GetValues<Permission>().ToList()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "DBA",
                DisplayName = "Database Administrator",
                Description = "Full migration access",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                Permissions = new List<Permission>
                {
                    Permission.ViewMigrations, Permission.ApplyMigrations, Permission.RollbackMigrations,
                    Permission.ViewEnvironments, Permission.ManageEnvironments,
                    Permission.ViewAuditLogs, Permission.ExportReports,
                    Permission.ScheduleMigrations, Permission.CancelScheduledMigrations,
                    Permission.ViewApprovalRequests, Permission.RequestApproval, Permission.ApproveProduction
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Developer",
                DisplayName = "Developer",
                Description = "Apply to non-production environments",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                Permissions = new List<Permission>
                {
                    Permission.ViewMigrations, Permission.ApplyMigrations,
                    Permission.ViewEnvironments,
                    Permission.ViewAuditLogs,
                    Permission.ScheduleMigrations, Permission.CancelScheduledMigrations,
                    Permission.ViewApprovalRequests, Permission.RequestApproval
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Viewer",
                DisplayName = "Viewer",
                Description = "Read-only access",
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                Permissions = new List<Permission>
                {
                    Permission.ViewMigrations, Permission.ViewEnvironments,
                    Permission.ViewAuditLogs,
                    Permission.ViewApprovalRequests
                }
            }
        };
    }

    #endregion

    #region Helpers

    private static void PopulateUserRoles(User user, UserEntity entity)
    {
        user.Roles = entity.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.ToDomainModel())
            .ToList();
        user.CalculateEffectivePermissions();
    }

    #endregion
}
