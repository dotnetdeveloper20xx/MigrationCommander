namespace MigrationCommander.Core.Models.Security;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username (unique).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Role IDs assigned to this user.
    /// </summary>
    public List<Guid> RoleIds { get; set; } = new();

    /// <summary>
    /// Roles assigned to this user (populated by service).
    /// </summary>
    public List<Role> Roles { get; set; } = new();

    /// <summary>
    /// All permissions the user has (union of all role permissions).
    /// </summary>
    public HashSet<Permission> EffectivePermissions { get; set; } = new();

    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// When the user was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// External identity provider ID (e.g., Azure AD, Okta).
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Notes about the user.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        return EffectivePermissions.Contains(permission);
    }

    /// <summary>
    /// Checks if the user has any of the specified permissions.
    /// </summary>
    public bool HasAnyPermission(params Permission[] permissions)
    {
        return permissions.Any(p => EffectivePermissions.Contains(p));
    }

    /// <summary>
    /// Checks if the user has all of the specified permissions.
    /// </summary>
    public bool HasAllPermissions(params Permission[] permissions)
    {
        return permissions.All(p => EffectivePermissions.Contains(p));
    }

    /// <summary>
    /// Calculates effective permissions from assigned roles.
    /// </summary>
    public void CalculateEffectivePermissions()
    {
        EffectivePermissions = Roles
            .SelectMany(r => r.Permissions)
            .ToHashSet();
    }
}
