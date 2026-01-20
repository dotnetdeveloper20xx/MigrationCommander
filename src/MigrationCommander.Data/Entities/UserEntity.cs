using MigrationCommander.Core.Models.Security;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing a user in the system.
/// </summary>
public class UserEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ExternalId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Navigation property for user roles (many-to-many).
    /// </summary>
    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();

    /// <summary>
    /// Converts to the domain model.
    /// </summary>
    public User ToDomainModel()
    {
        return new User
        {
            Id = Id,
            Username = Username,
            Email = Email,
            DisplayName = DisplayName,
            IsActive = IsActive,
            ExternalId = ExternalId,
            Notes = Notes,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            LastLoginAt = LastLoginAt,
            RoleIds = UserRoles.Select(ur => ur.RoleId).ToList()
        };
    }

    /// <summary>
    /// Creates from a domain model.
    /// </summary>
    public static UserEntity FromDomainModel(User user)
    {
        return new UserEntity
        {
            Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            ExternalId = user.ExternalId,
            Notes = user.Notes,
            CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
