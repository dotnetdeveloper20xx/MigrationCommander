namespace MigrationCommander.Extensions;

/// <summary>
/// Configuration options for MigrationCommander.
/// </summary>
public class MigrationCommanderOptions
{
    /// <summary>
    /// Path to the internal SQLite database for storing configuration and audit logs.
    /// Defaults to "migration_commander.db" in the application directory.
    /// </summary>
    public string? InternalDatabasePath { get; set; }

    /// <summary>
    /// The target DbContext type to manage migrations for.
    /// </summary>
    public Type? TargetDbContextType { get; set; }

    /// <summary>
    /// If true, requires explicit approval before executing migrations on production environments.
    /// Default: true.
    /// </summary>
    public bool RequireApprovalForProduction { get; set; } = true;

    /// <summary>
    /// If true, automatically creates a backup before executing destructive migrations.
    /// Default: true.
    /// </summary>
    public bool AutoBackupBeforeDestructive { get; set; } = true;

    /// <summary>
    /// Default timeout for migration execution.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of concurrent migration operations.
    /// Default: 1 (sequential execution).
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 1;

    /// <summary>
    /// If true, enables SignalR for real-time updates.
    /// Default: true.
    /// </summary>
    public bool EnableRealTimeUpdates { get; set; } = true;

    /// <summary>
    /// Number of days to retain audit logs.
    /// Default: 365 days. Set to 0 for no retention limit.
    /// </summary>
    public int AuditLogRetentionDays { get; set; } = 365;

    /// <summary>
    /// Application name used for data protection keys.
    /// </summary>
    public string ApplicationName { get; set; } = "MigrationCommander";

    /// <summary>
    /// Path for storing data protection keys.
    /// If null, keys are stored in the default location.
    /// </summary>
    public string? DataProtectionKeyPath { get; set; }
}
