namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents the analysis of a rollback operation before execution.
/// </summary>
public class RollbackAnalysis
{
    /// <summary>
    /// The ID of the migration being analyzed for rollback.
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// The environment where the rollback would be performed.
    /// </summary>
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// Indicates if the rollback can be performed.
    /// </summary>
    public bool CanRollback { get; set; }

    /// <summary>
    /// Reason why rollback is blocked (if applicable).
    /// </summary>
    public string? BlockingReason { get; set; }

    /// <summary>
    /// List of tables impacted by the rollback.
    /// </summary>
    public List<TableImpact> AffectedTables { get; set; } = new();

    /// <summary>
    /// Total number of rows affected by the rollback.
    /// </summary>
    public long TotalRowsAffected { get; set; }

    /// <summary>
    /// Indicates if the rollback will delete data.
    /// </summary>
    public bool WillDeleteData { get; set; }

    /// <summary>
    /// Indicates if the rollback will drop tables.
    /// </summary>
    public bool WillDropTables { get; set; }

    /// <summary>
    /// Indicates if the rollback will drop columns.
    /// </summary>
    public bool WillDropColumns { get; set; }

    /// <summary>
    /// List of migrations that depend on this migration.
    /// </summary>
    public List<string> DependentMigrations { get; set; } = new();

    /// <summary>
    /// List of database objects that depend on objects created by this migration.
    /// </summary>
    public List<string> DependentObjects { get; set; } = new();

    /// <summary>
    /// Warnings about the rollback operation.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// The assessed risk level of the rollback.
    /// </summary>
    public RollbackRiskLevel RiskLevel { get; set; }

    /// <summary>
    /// The DOWN SQL that would be executed.
    /// </summary>
    public string? DownSql { get; set; }

    /// <summary>
    /// Estimated duration of the rollback based on row counts and operations.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; set; }
}
