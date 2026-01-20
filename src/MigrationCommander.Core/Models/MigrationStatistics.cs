namespace MigrationCommander.Core.Models;

/// <summary>
/// Aggregated statistics about migrations.
/// </summary>
public class MigrationStatistics
{
    /// <summary>
    /// Total number of migrations discovered.
    /// </summary>
    public int TotalMigrations { get; set; }

    /// <summary>
    /// Number of successful migration executions.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed migration executions.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Number of rolled back migrations.
    /// </summary>
    public int RolledBackMigrations { get; set; }

    /// <summary>
    /// Number of pending migrations across all environments.
    /// </summary>
    public int PendingMigrations { get; set; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate => TotalExecutions > 0
        ? Math.Round((double)SuccessfulExecutions / TotalExecutions * 100, 2)
        : 0;

    /// <summary>
    /// Total number of executions (successful + failed).
    /// </summary>
    public int TotalExecutions => SuccessfulExecutions + FailedExecutions;

    /// <summary>
    /// Average execution time for migrations.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }

    /// <summary>
    /// Fastest migration execution time.
    /// </summary>
    public TimeSpan FastestExecution { get; set; }

    /// <summary>
    /// Slowest migration execution time.
    /// </summary>
    public TimeSpan SlowestExecution { get; set; }

    /// <summary>
    /// Daily trend data for the statistics period.
    /// </summary>
    public List<ExecutionTrend> DailyTrends { get; set; } = new();

    /// <summary>
    /// Statistics broken down by environment.
    /// </summary>
    public List<EnvironmentStatistics> EnvironmentBreakdown { get; set; } = new();

    /// <summary>
    /// Most active users.
    /// </summary>
    public List<UserActivitySummary> TopUsers { get; set; } = new();

    /// <summary>
    /// Start of the statistics period.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of the statistics period.
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Statistics for a specific environment.
/// </summary>
public class EnvironmentStatistics
{
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public ProviderType Provider { get; set; }
    public int AppliedMigrations { get; set; }
    public int PendingMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public DateTime? LastMigrationAt { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Summary of user activity.
/// </summary>
public class UserActivitySummary
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int MigrationsApplied { get; set; }
    public int MigrationsRolledBack { get; set; }
    public int ScheduledMigrations { get; set; }
    public DateTime LastActivityAt { get; set; }
}
