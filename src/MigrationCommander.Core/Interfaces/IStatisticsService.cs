using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for retrieving migration statistics and trend data.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Gets overall migration statistics for the specified period.
    /// </summary>
    /// <param name="from">Start of period (null = all time).</param>
    /// <param name="to">End of period (null = now).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated statistics.</returns>
    Task<MigrationStatistics> GetOverallStatisticsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for a specific environment.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="from">Start of period (null = all time).</param>
    /// <param name="to">End of period (null = now).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Environment-specific statistics.</returns>
    Task<EnvironmentStatistics> GetEnvironmentStatisticsAsync(
        Guid environmentId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity statistics for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="from">Start of period (null = all time).</param>
    /// <param name="to">End of period (null = now).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User activity summary.</returns>
    Task<UserActivitySummary> GetUserActivityAsync(
        string userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trend data for the specified period.
    /// </summary>
    /// <param name="from">Start of period.</param>
    /// <param name="to">End of period.</param>
    /// <param name="granularity">Data point granularity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trend data points.</returns>
    Task<IReadOnlyList<ExecutionTrend>> GetTrendDataAsync(
        DateTime from,
        DateTime to,
        TrendGranularity granularity = TrendGranularity.Daily,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top active users.
    /// </summary>
    /// <param name="count">Number of users to return.</param>
    /// <param name="from">Start of period (null = all time).</param>
    /// <param name="to">End of period (null = now).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user activity summaries.</returns>
    Task<IReadOnlyList<UserActivitySummary>> GetTopUsersAsync(
        int count = 10,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
