namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents a configured database environment (e.g., Development, Staging, Production).
/// </summary>
public class DatabaseEnvironment
{
    /// <summary>
    /// Unique identifier for the environment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Internal name for the environment (e.g., "production", "staging").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly display name (e.g., "Production Server", "Staging Environment").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The database provider type for this environment.
    /// </summary>
    public ProviderType Provider { get; set; }

    /// <summary>
    /// The connection string for this environment (should be encrypted at rest).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a production environment (enables extra safety checks).
    /// </summary>
    public bool IsProduction { get; set; }

    /// <summary>
    /// Indicates if migrations require approval before execution.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Indicates if this environment is active and available for operations.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of migrations currently applied to this environment.
    /// </summary>
    public int AppliedMigrationCount { get; set; }

    /// <summary>
    /// Number of migrations pending for this environment.
    /// </summary>
    public int PendingMigrationCount { get; set; }

    /// <summary>
    /// Timestamp of the last migration operation.
    /// </summary>
    public DateTime? LastMigrationAt { get; set; }

    /// <summary>
    /// User who performed the last migration.
    /// </summary>
    public string? LastMigrationBy { get; set; }

    /// <summary>
    /// Last error message if a migration failed.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When this environment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the environment status was last checked.
    /// </summary>
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>
    /// Optional color for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Sort order for display purposes.
    /// </summary>
    public int SortOrder { get; set; }
}
