namespace MigrationCommander.Core.Models.Security;

/// <summary>
/// Represents a role that groups permissions together.
/// </summary>
public class Role
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role name (unique).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the role.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Permissions assigned to this role.
    /// </summary>
    public List<Permission> Permissions { get; set; } = new();

    /// <summary>
    /// Whether this is a built-in role (cannot be deleted).
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// When the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the role was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the predefined roles.
    /// </summary>
    public static IReadOnlyList<Role> GetBuiltInRoles() => new List<Role>
    {
        new()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Name = "viewer",
            DisplayName = "Viewer",
            Description = "Read-only access to view migrations and environments",
            IsBuiltIn = true,
            Permissions = new List<Permission>
            {
                Permission.ViewMigrations,
                Permission.ViewEnvironments,
                Permission.ViewAuditLogs,
                Permission.ViewReports,
                Permission.ViewApprovalRequests,
                Permission.ViewSettings
            }
        },
        new()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            Name = "developer",
            DisplayName = "Developer",
            Description = "Can apply migrations to non-production environments",
            IsBuiltIn = true,
            Permissions = new List<Permission>
            {
                Permission.ViewMigrations,
                Permission.ApplyMigrations,
                Permission.RollbackMigrations,
                Permission.ScheduleMigrations,
                Permission.CancelScheduledMigrations,
                Permission.ViewEnvironments,
                Permission.TestConnections,
                Permission.ViewAuditLogs,
                Permission.ViewReports,
                Permission.RequestApproval,
                Permission.ViewApprovalRequests,
                Permission.ViewSettings
            }
        },
        new()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000003"),
            Name = "dba",
            DisplayName = "Database Administrator",
            Description = "Full migration access including production",
            IsBuiltIn = true,
            Permissions = new List<Permission>
            {
                Permission.ViewMigrations,
                Permission.ApplyMigrations,
                Permission.ApplyMigrationsToProduction,
                Permission.RollbackMigrations,
                Permission.RollbackMigrationsFromProduction,
                Permission.ScheduleMigrations,
                Permission.CancelScheduledMigrations,
                Permission.ViewEnvironments,
                Permission.ManageEnvironments,
                Permission.TestConnections,
                Permission.ViewAuditLogs,
                Permission.ExportAuditLogs,
                Permission.ViewReports,
                Permission.ExportReports,
                Permission.RequestApproval,
                Permission.ApproveProduction,
                Permission.RejectApproval,
                Permission.ViewApprovalRequests,
                Permission.ViewSettings
            }
        },
        new()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000004"),
            Name = "admin",
            DisplayName = "Administrator",
            Description = "Full system access",
            IsBuiltIn = true,
            Permissions = Enum.GetValues<Permission>().ToList()
        }
    };
}
