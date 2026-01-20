using MigrationCommander.Core.Models.Security;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for managing users, roles, and authorization.
/// </summary>
public interface IAuthorizationService
{
    #region User Management

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users.
    /// </summary>
    Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns roles to a user.
    /// </summary>
    Task AssignRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes roles from a user.
    /// </summary>
    Task RemoveRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    #endregion

    #region Role Management

    /// <summary>
    /// Gets a role by ID.
    /// </summary>
    Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by name.
    /// </summary>
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    Task UpdateRoleAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role (only non-built-in roles can be deleted).
    /// </summary>
    Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    #endregion

    #region Authorization Checks

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any of the specified permissions.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has all of the specified permissions.
    /// </summary>
    Task<bool> HasAllPermissionsAsync(Guid userId, IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can apply migrations to a specific environment.
    /// </summary>
    Task<bool> CanApplyMigrationAsync(Guid userId, Guid environmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can rollback migrations from a specific environment.
    /// </summary>
    Task<bool> CanRollbackMigrationAsync(Guid userId, Guid environmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user.
    /// </summary>
    Task<HashSet<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Session Management

    /// <summary>
    /// Records a user login.
    /// </summary>
    Task RecordLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a user from external identity provider claims.
    /// </summary>
    Task<User> GetOrCreateFromExternalAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    #endregion
}
