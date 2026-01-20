namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents execution trends over a time period.
/// </summary>
public class ExecutionTrend
{
    /// <summary>
    /// The date of this trend data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of successful executions on this date.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed executions on this date.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of rollbacks on this date.
    /// </summary>
    public int RollbackCount { get; set; }

    /// <summary>
    /// Total executions on this date.
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;

    /// <summary>
    /// Success rate for this date.
    /// </summary>
    public double SuccessRate => TotalCount > 0
        ? Math.Round((double)SuccessCount / TotalCount * 100, 2)
        : 0;

    /// <summary>
    /// Average execution time on this date.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }
}

/// <summary>
/// Granularity for trend data.
/// </summary>
public enum TrendGranularity
{
    /// <summary>
    /// Hourly data points.
    /// </summary>
    Hourly,

    /// <summary>
    /// Daily data points.
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly data points.
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly data points.
    /// </summary>
    Monthly
}
