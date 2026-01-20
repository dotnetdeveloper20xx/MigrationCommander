using MigrationCommander.Core.Models.Security;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing a role in the system.
/// </summary>
public class RoleEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsBuiltIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Comma-separated list of permission values.
    /// </summary>
    public string PermissionsJson { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for user roles (many-to-many).
    /// </summary>
    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();

    /// <summary>
    /// Converts to the domain model.
    /// </summary>
    public Role ToDomainModel()
    {
        var permissions = new List<Permission>();
        if (!string.IsNullOrEmpty(PermissionsJson))
        {
            var permissionStrings = PermissionsJson.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var permStr in permissionStrings)
            {
                if (Enum.TryParse<Permission>(permStr.Trim(), out var perm))
                {
                    permissions.Add(perm);
                }
            }
        }

        return new Role
        {
            Id = Id,
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            IsBuiltIn = IsBuiltIn,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Permissions = permissions
        };
    }

    /// <summary>
    /// Creates from a domain model.
    /// </summary>
    public static RoleEntity FromDomainModel(Role role)
    {
        return new RoleEntity
        {
            Id = role.Id == Guid.Empty ? Guid.NewGuid() : role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt == default ? DateTime.UtcNow : role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            PermissionsJson = string.Join(",", role.Permissions.Select(p => p.ToString()))
        };
    }
}
