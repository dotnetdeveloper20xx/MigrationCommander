using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models.Security;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for managing users, roles, and authorization.
/// Uses database storage via UserRepository.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly DatabaseRepository _databaseRepository;
    private readonly UserRepository _userRepository;

    public AuthorizationService(DatabaseRepository databaseRepository, UserRepository userRepository)
    {
        _databaseRepository = databaseRepository;
        _userRepository = userRepository;
    }

    #region User Management

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetUserByIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetUserByUsernameAsync(username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetUserByEmailAsync(email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetAllUsersAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(user.Username))
            throw new ArgumentException("Username is required", nameof(user));

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email is required", nameof(user));

        // Check for duplicates
        var existingByUsername = await _userRepository.GetUserByUsernameAsync(user.Username, cancellationToken);
        if (existingByUsername != null)
        {
            throw new InvalidOperationException($"A user with username '{user.Username}' already exists");
        }

        var existingByEmail = await _userRepository.GetUserByEmailAsync(user.Email, cancellationToken);
        if (existingByEmail != null)
        {
            throw new InvalidOperationException($"A user with email '{user.Email}' already exists");
        }

        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
        }

        user.CreatedAt = DateTime.UtcNow;

        var createdUser = await _userRepository.AddUserAsync(user, cancellationToken);

        // Assign initial roles if specified
        if (user.RoleIds.Any())
        {
            await _userRepository.SetUserRolesAsync(createdUser.Id, user.RoleIds, null, cancellationToken);
            createdUser = await _userRepository.GetUserByIdAsync(createdUser.Id, cancellationToken);
        }

        return createdUser!;
    }

    /// <inheritdoc />
    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var existing = await _userRepository.GetUserByIdAsync(user.Id, cancellationToken);
        if (existing == null)
        {
            throw new InvalidOperationException($"User with ID '{user.Id}' not found");
        }

        // Check for duplicate username/email (excluding self)
        var existingByUsername = await _userRepository.GetUserByUsernameAsync(user.Username, cancellationToken);
        if (existingByUsername != null && existingByUsername.Id != user.Id)
        {
            throw new InvalidOperationException($"A user with username '{user.Username}' already exists");
        }

        var existingByEmail = await _userRepository.GetUserByEmailAsync(user.Email, cancellationToken);
        if (existingByEmail != null && existingByEmail.Id != user.Id)
        {
            throw new InvalidOperationException($"A user with email '{user.Email}' already exists");
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _userRepository.DeleteUserAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        foreach (var roleId in roleIds)
        {
            var role = await _userRepository.GetRoleByIdAsync(roleId, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID '{roleId}' not found");
            }

            await _userRepository.AssignRoleAsync(userId, roleId, null, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RemoveRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        foreach (var roleId in roleIds)
        {
            await _userRepository.RevokeRoleAsync(userId, roleId, cancellationToken);
        }
    }

    #endregion

    #region Role Management

    /// <inheritdoc />
    public async Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetRoleByIdAsync(roleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetRoleByNameAsync(name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetAllRolesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        if (string.IsNullOrWhiteSpace(role.Name))
            throw new ArgumentException("Role name is required", nameof(role));

        var existingRole = await _userRepository.GetRoleByNameAsync(role.Name, cancellationToken);
        if (existingRole != null)
        {
            throw new InvalidOperationException($"A role with name '{role.Name}' already exists");
        }

        if (role.Id == Guid.Empty)
        {
            role.Id = Guid.NewGuid();
        }

        role.CreatedAt = DateTime.UtcNow;
        role.IsBuiltIn = false;

        return await _userRepository.AddRoleAsync(role, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        var existingRole = await _userRepository.GetRoleByIdAsync(role.Id, cancellationToken);
        if (existingRole == null)
        {
            throw new InvalidOperationException($"Role with ID '{role.Id}' not found");
        }

        if (existingRole.IsBuiltIn)
        {
            throw new InvalidOperationException("Built-in roles cannot be modified");
        }

        role.UpdatedAt = DateTime.UtcNow;
        role.IsBuiltIn = false;

        await _userRepository.UpdateRoleAsync(role, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _userRepository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role != null)
        {
            if (role.IsBuiltIn)
            {
                throw new InvalidOperationException("Built-in roles cannot be deleted");
            }

            await _userRepository.DeleteRoleAsync(roleId, cancellationToken);
        }
    }

    #endregion

    #region Authorization Checks

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(Guid userId, Permission permission, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        return user.HasPermission(permission);
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<Permission> permissions, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        return user.HasAnyPermission(permissions.ToArray());
    }

    /// <inheritdoc />
    public async Task<bool> HasAllPermissionsAsync(Guid userId, IEnumerable<Permission> permissions, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        return user.HasAllPermissions(permissions.ToArray());
    }

    /// <inheritdoc />
    public async Task<bool> CanApplyMigrationAsync(Guid userId, Guid environmentId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        // Check if environment is production
        var environment = await _databaseRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            return false;
        }

        if (environment.IsProduction)
        {
            return user.HasPermission(Permission.ApplyMigrationsToProduction);
        }

        return user.HasPermission(Permission.ApplyMigrations);
    }

    /// <inheritdoc />
    public async Task<bool> CanRollbackMigrationAsync(Guid userId, Guid environmentId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        var environment = await _databaseRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            return false;
        }

        if (environment.IsProduction)
        {
            return user.HasPermission(Permission.RollbackMigrationsFromProduction);
        }

        return user.HasPermission(Permission.RollbackMigrations);
    }

    /// <inheritdoc />
    public async Task<HashSet<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return new HashSet<Permission>();
        }

        return user.EffectivePermissions;
    }

    #endregion

    #region Session Management

    /// <inheritdoc />
    public async Task RecordLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _userRepository.UpdateLastLoginAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User> GetOrCreateFromExternalAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID is required", nameof(externalId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        // Try to find by external ID first
        var user = await _userRepository.GetUserByExternalIdAsync(externalId, cancellationToken);
        if (user != null)
        {
            // Update last login
            await RecordLoginAsync(user.Id, cancellationToken);
            return user;
        }

        // Try to find by email
        user = await _userRepository.GetUserByEmailAsync(email, cancellationToken);
        if (user != null)
        {
            // Link external ID
            user.ExternalId = externalId;
            await _userRepository.UpdateUserAsync(user, cancellationToken);
            await RecordLoginAsync(user.Id, cancellationToken);
            return await _userRepository.GetUserByIdAsync(user.Id, cancellationToken) ?? user;
        }

        // Create new user
        var viewerRole = await _userRepository.GetRoleByNameAsync("Viewer", cancellationToken);
        user = new User
        {
            Id = Guid.NewGuid(),
            Username = email.Split('@')[0],
            Email = email,
            DisplayName = displayName,
            ExternalId = externalId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddUserAsync(user, cancellationToken);

        if (viewerRole != null)
        {
            await _userRepository.AssignRoleAsync(createdUser.Id, viewerRole.Id, null, cancellationToken);
            createdUser = await _userRepository.GetUserByIdAsync(createdUser.Id, cancellationToken);
        }

        await RecordLoginAsync(createdUser!.Id, cancellationToken);

        return createdUser;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Ensures default roles exist in the database.
    /// Should be called during application startup.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _userRepository.SeedDefaultRolesAsync(cancellationToken);
    }

    #endregion
}
