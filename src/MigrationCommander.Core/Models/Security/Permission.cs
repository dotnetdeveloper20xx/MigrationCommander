namespace MigrationCommander.Core.Models.Security;

/// <summary>
/// Defines available permissions in the system.
/// </summary>
public enum Permission
{
    // Migration permissions
    ViewMigrations = 100,
    ApplyMigrations = 101,
    ApplyMigrationsToProduction = 102,
    RollbackMigrations = 103,
    RollbackMigrationsFromProduction = 104,
    ScheduleMigrations = 105,
    CancelScheduledMigrations = 106,

    // Environment permissions
    ViewEnvironments = 200,
    ManageEnvironments = 201,
    TestConnections = 202,

    // Audit permissions
    ViewAuditLogs = 300,
    ExportAuditLogs = 301,

    // Report permissions
    ViewReports = 400,
    ExportReports = 401,

    // Approval permissions
    RequestApproval = 500,
    ApproveProduction = 501,
    RejectApproval = 502,
    ViewApprovalRequests = 503,

    // User management permissions
    ViewUsers = 600,
    ManageUsers = 601,
    AssignRoles = 602,

    // System permissions
    ViewSettings = 700,
    ManageSettings = 701,
    ViewSystemHealth = 702
}

/// <summary>
/// Metadata about a permission.
/// </summary>
public class PermissionInfo
{
    public Permission Permission { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public static IReadOnlyList<PermissionInfo> GetAllPermissions() => new List<PermissionInfo>
    {
        // Migrations
        new() { Permission = Permission.ViewMigrations, Name = "View Migrations", Description = "View migration list and status", Category = "Migrations" },
        new() { Permission = Permission.ApplyMigrations, Name = "Apply Migrations", Description = "Apply migrations to non-production environments", Category = "Migrations" },
        new() { Permission = Permission.ApplyMigrationsToProduction, Name = "Apply to Production", Description = "Apply migrations to production environments", Category = "Migrations" },
        new() { Permission = Permission.RollbackMigrations, Name = "Rollback Migrations", Description = "Rollback migrations from non-production environments", Category = "Migrations" },
        new() { Permission = Permission.RollbackMigrationsFromProduction, Name = "Rollback from Production", Description = "Rollback migrations from production environments", Category = "Migrations" },
        new() { Permission = Permission.ScheduleMigrations, Name = "Schedule Migrations", Description = "Schedule migrations for future execution", Category = "Migrations" },
        new() { Permission = Permission.CancelScheduledMigrations, Name = "Cancel Schedules", Description = "Cancel scheduled migrations", Category = "Migrations" },

        // Environments
        new() { Permission = Permission.ViewEnvironments, Name = "View Environments", Description = "View environment configurations", Category = "Environments" },
        new() { Permission = Permission.ManageEnvironments, Name = "Manage Environments", Description = "Add, edit, and delete environments", Category = "Environments" },
        new() { Permission = Permission.TestConnections, Name = "Test Connections", Description = "Test database connections", Category = "Environments" },

        // Audit
        new() { Permission = Permission.ViewAuditLogs, Name = "View Audit Logs", Description = "View audit log entries", Category = "Audit" },
        new() { Permission = Permission.ExportAuditLogs, Name = "Export Audit Logs", Description = "Export audit logs to file", Category = "Audit" },

        // Reports
        new() { Permission = Permission.ViewReports, Name = "View Reports", Description = "View reports and statistics", Category = "Reports" },
        new() { Permission = Permission.ExportReports, Name = "Export Reports", Description = "Export reports to various formats", Category = "Reports" },

        // Approvals
        new() { Permission = Permission.RequestApproval, Name = "Request Approval", Description = "Request approval for production changes", Category = "Approvals" },
        new() { Permission = Permission.ApproveProduction, Name = "Approve Production", Description = "Approve production migration requests", Category = "Approvals" },
        new() { Permission = Permission.RejectApproval, Name = "Reject Approval", Description = "Reject approval requests", Category = "Approvals" },
        new() { Permission = Permission.ViewApprovalRequests, Name = "View Approvals", Description = "View approval requests", Category = "Approvals" },

        // Users
        new() { Permission = Permission.ViewUsers, Name = "View Users", Description = "View user list and roles", Category = "Users" },
        new() { Permission = Permission.ManageUsers, Name = "Manage Users", Description = "Add, edit, and delete users", Category = "Users" },
        new() { Permission = Permission.AssignRoles, Name = "Assign Roles", Description = "Assign roles to users", Category = "Users" },

        // System
        new() { Permission = Permission.ViewSettings, Name = "View Settings", Description = "View application settings", Category = "System" },
        new() { Permission = Permission.ManageSettings, Name = "Manage Settings", Description = "Modify application settings", Category = "System" },
        new() { Permission = Permission.ViewSystemHealth, Name = "View System Health", Description = "View system health and diagnostics", Category = "System" }
    };
}
