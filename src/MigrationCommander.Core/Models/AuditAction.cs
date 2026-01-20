namespace MigrationCommander.Core.Models;

/// <summary>
/// Types of actions that can be audited.
/// </summary>
public enum AuditAction
{
    // Migration actions
    ViewedMigration,
    PreviewedSql,
    AppliedMigration,
    RolledBackMigration,
    ScheduledMigration,
    CancelledSchedule,

    // Environment actions
    AddedEnvironment,
    ModifiedEnvironment,
    RemovedEnvironment,
    TestedConnection,

    // Report actions
    ExportedReport,

    // Approval workflow actions
    ApprovalRequested,
    ApprovalGranted,
    ApprovalDenied,
    ApprovalCancelled,

    // Security actions
    UserCreated,
    UserModified,
    UserDeleted,
    RoleAssigned,
    RoleRevoked,
    LoginSuccess,
    LoginFailed,
    PermissionDenied
}
