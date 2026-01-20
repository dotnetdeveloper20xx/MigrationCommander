namespace MigrationCommander.Core.Models;

/// <summary>
/// Result of comparing migration status across multiple environments.
/// </summary>
public class EnvironmentComparisonResult
{
    /// <summary>
    /// All migrations discovered across all environments.
    /// </summary>
    public List<string> AllMigrations { get; set; } = new();

    /// <summary>
    /// Status of each migration in each environment.
    /// Key: Migration ID, Value: Dictionary of Environment ID to status.
    /// </summary>
    public Dictionary<string, Dictionary<Guid, MigrationStatus>> MigrationStatuses { get; set; } = new();

    /// <summary>
    /// Environments included in the comparison.
    /// </summary>
    public List<EnvironmentSummary> Environments { get; set; } = new();

    /// <summary>
    /// Migrations that are out of sync across environments.
    /// </summary>
    public List<MigrationSyncIssue> SyncIssues { get; set; } = new();
}

/// <summary>
/// Summary information about an environment for comparison purposes.
/// </summary>
public class EnvironmentSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ProviderType Provider { get; set; }
    public int AppliedCount { get; set; }
    public int PendingCount { get; set; }
    public DateTime? LastMigrationAt { get; set; }
}

/// <summary>
/// Represents a migration that is out of sync across environments.
/// </summary>
public class MigrationSyncIssue
{
    public string MigrationId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> AffectedEnvironments { get; set; } = new();
    public SyncIssueType IssueType { get; set; }
}

/// <summary>
/// Types of synchronization issues.
/// </summary>
public enum SyncIssueType
{
    /// <summary>
    /// Migration is applied in some environments but not others.
    /// </summary>
    PartiallyApplied,

    /// <summary>
    /// Migration failed in one or more environments.
    /// </summary>
    FailedInSome,

    /// <summary>
    /// Migration was rolled back in some environments.
    /// </summary>
    RolledBackInSome
}
