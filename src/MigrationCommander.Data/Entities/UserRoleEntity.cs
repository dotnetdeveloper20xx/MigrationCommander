namespace MigrationCommander.Data.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between users and roles.
/// </summary>
public class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public UserEntity? User { get; set; }

    /// <summary>
    /// Navigation property to the role.
    /// </summary>
    public RoleEntity? Role { get; set; }
}
