using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for retrieving migration statistics and trend data.
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly AuditRepository _auditRepository;
    private readonly HistoryRepository _historyRepository;
    private readonly DatabaseRepository _databaseRepository;

    public StatisticsService(
        AuditRepository auditRepository,
        HistoryRepository historyRepository,
        DatabaseRepository databaseRepository)
    {
        _auditRepository = auditRepository;
        _historyRepository = historyRepository;
        _databaseRepository = databaseRepository;
    }

    /// <inheritdoc />
    public async Task<MigrationStatistics> GetOverallStatisticsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var periodStart = from ?? DateTime.UtcNow.AddMonths(-1);
        var periodEnd = to ?? DateTime.UtcNow;

        var filter = new AuditLogFilter
        {
            FromDate = periodStart,
            ToDate = periodEnd,
            Skip = 0,
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(filter, cancellationToken);

        var appliedLogs = logs.Where(l => l.Action == AuditAction.AppliedMigration).ToList();
        var rolledBackLogs = logs.Where(l => l.Action == AuditAction.RolledBackMigration).ToList();

        var successfulExecutions = appliedLogs.Count(l => l.Success);
        var failedExecutions = appliedLogs.Count(l => !l.Success);
        var rolledBackCount = rolledBackLogs.Count(l => l.Success);

        var executionTimes = appliedLogs
            .Where(l => l.Success && l.DurationTicks > 0)
            .Select(l => TimeSpan.FromTicks(l.DurationTicks))
            .ToList();

        var stats = new MigrationStatistics
        {
            TotalMigrations = appliedLogs.Select(l => l.MigrationId).Distinct().Count(),
            SuccessfulExecutions = successfulExecutions,
            FailedExecutions = failedExecutions,
            RolledBackMigrations = rolledBackCount,
            PendingMigrations = 0, // Would need discovery service to get pending
            AverageExecutionTime = executionTimes.Any()
                ? TimeSpan.FromTicks((long)executionTimes.Average(t => t.Ticks))
                : TimeSpan.Zero,
            FastestExecution = executionTimes.Any()
                ? executionTimes.Min()
                : TimeSpan.Zero,
            SlowestExecution = executionTimes.Any()
                ? executionTimes.Max()
                : TimeSpan.Zero,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };

        // Get daily trends
        stats.DailyTrends = await GetTrendDataInternalAsync(appliedLogs, rolledBackLogs, periodStart, periodEnd, TrendGranularity.Daily);

        // Get environment breakdown
        stats.EnvironmentBreakdown = await GetEnvironmentBreakdownAsync(appliedLogs, cancellationToken);

        // Get top users
        stats.TopUsers = GetTopUsersFromLogs(appliedLogs, rolledBackLogs, 5);

        return stats;
    }

    /// <inheritdoc />
    public async Task<EnvironmentStatistics> GetEnvironmentStatisticsAsync(
        Guid environmentId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var periodStart = from ?? DateTime.UtcNow.AddMonths(-1);
        var periodEnd = to ?? DateTime.UtcNow;

        var environment = await _databaseRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            return new EnvironmentStatistics
            {
                EnvironmentId = environmentId,
                EnvironmentName = "Unknown"
            };
        }

        var filter = new AuditLogFilter
        {
            FromDate = periodStart,
            ToDate = periodEnd,
            EnvironmentId = environmentId,
            Skip = 0,
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(filter, cancellationToken);
        var appliedLogs = logs.Where(l => l.Action == AuditAction.AppliedMigration).ToList();

        var successfulExecutions = appliedLogs.Count(l => l.Success);
        var failedExecutions = appliedLogs.Count(l => !l.Success);
        var totalExecutions = successfulExecutions + failedExecutions;

        var histories = await _historyRepository.GetByEnvironmentAsync(environmentId, cancellationToken);
        var pendingCount = histories.Count(h => h.Status == MigrationStatus.Pending);

        return new EnvironmentStatistics
        {
            EnvironmentId = environmentId,
            EnvironmentName = environment.DisplayName,
            Provider = environment.Provider,
            AppliedMigrations = successfulExecutions,
            PendingMigrations = pendingCount,
            FailedMigrations = failedExecutions,
            LastMigrationAt = appliedLogs.Any() ? appliedLogs.Max(l => l.Timestamp) : null,
            SuccessRate = totalExecutions > 0
                ? Math.Round((double)successfulExecutions / totalExecutions * 100, 2)
                : 0
        };
    }

    /// <inheritdoc />
    public async Task<UserActivitySummary> GetUserActivityAsync(
        string userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var periodStart = from ?? DateTime.UtcNow.AddMonths(-1);
        var periodEnd = to ?? DateTime.UtcNow;

        var filter = new AuditLogFilter
        {
            FromDate = periodStart,
            ToDate = periodEnd,
            UserId = userId,
            Skip = 0,
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(filter, cancellationToken);

        var appliedLogs = logs.Where(l => l.Action == AuditAction.AppliedMigration && l.Success).ToList();
        var rolledBackLogs = logs.Where(l => l.Action == AuditAction.RolledBackMigration && l.Success).ToList();
        var scheduledLogs = logs.Where(l => l.Action == AuditAction.ScheduledMigration).ToList();

        var allUserLogs = logs.ToList();
        var lastActivity = allUserLogs.Any() ? allUserLogs.Max(l => l.Timestamp) : DateTime.UtcNow;
        var userEmail = allUserLogs.FirstOrDefault()?.UserEmail ?? string.Empty;

        return new UserActivitySummary
        {
            UserId = userId,
            UserEmail = userEmail,
            MigrationsApplied = appliedLogs.Count,
            MigrationsRolledBack = rolledBackLogs.Count,
            ScheduledMigrations = scheduledLogs.Count,
            LastActivityAt = lastActivity
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutionTrend>> GetTrendDataAsync(
        DateTime from,
        DateTime to,
        TrendGranularity granularity = TrendGranularity.Daily,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditLogFilter
        {
            FromDate = from,
            ToDate = to,
            Skip = 0,
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(filter, cancellationToken);
        var appliedLogs = logs.Where(l => l.Action == AuditAction.AppliedMigration).ToList();
        var rolledBackLogs = logs.Where(l => l.Action == AuditAction.RolledBackMigration).ToList();

        return await GetTrendDataInternalAsync(appliedLogs, rolledBackLogs, from, to, granularity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserActivitySummary>> GetTopUsersAsync(
        int count = 10,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var periodStart = from ?? DateTime.UtcNow.AddMonths(-1);
        var periodEnd = to ?? DateTime.UtcNow;

        var filter = new AuditLogFilter
        {
            FromDate = periodStart,
            ToDate = periodEnd,
            Skip = 0,
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(filter, cancellationToken);
        var appliedLogs = logs.Where(l => l.Action == AuditAction.AppliedMigration && l.Success).ToList();
        var rolledBackLogs = logs.Where(l => l.Action == AuditAction.RolledBackMigration && l.Success).ToList();

        return GetTopUsersFromLogs(appliedLogs, rolledBackLogs, count);
    }

    private Task<List<ExecutionTrend>> GetTrendDataInternalAsync(
        List<Data.Entities.AuditLog> appliedLogs,
        List<Data.Entities.AuditLog> rolledBackLogs,
        DateTime from,
        DateTime to,
        TrendGranularity granularity)
    {
        var trends = new List<ExecutionTrend>();

        var dateRange = GetDateRange(from, to, granularity);

        foreach (var date in dateRange)
        {
            var nextDate = GetNextDate(date, granularity);

            var periodApplied = appliedLogs
                .Where(l => l.Timestamp >= date && l.Timestamp < nextDate)
                .ToList();

            var periodRollbacks = rolledBackLogs
                .Where(l => l.Timestamp >= date && l.Timestamp < nextDate)
                .ToList();

            var successCount = periodApplied.Count(l => l.Success);
            var failureCount = periodApplied.Count(l => !l.Success);
            var rollbackCount = periodRollbacks.Count(l => l.Success);

            var executionTimes = periodApplied
                .Where(l => l.Success && l.DurationTicks > 0)
                .Select(l => TimeSpan.FromTicks(l.DurationTicks))
                .ToList();

            trends.Add(new ExecutionTrend
            {
                Date = date,
                SuccessCount = successCount,
                FailureCount = failureCount,
                RollbackCount = rollbackCount,
                AverageExecutionTime = executionTimes.Any()
                    ? TimeSpan.FromTicks((long)executionTimes.Average(t => t.Ticks))
                    : TimeSpan.Zero
            });
        }

        return Task.FromResult(trends);
    }

    private async Task<List<EnvironmentStatistics>> GetEnvironmentBreakdownAsync(
        List<Data.Entities.AuditLog> appliedLogs,
        CancellationToken cancellationToken)
    {
        var environments = await _databaseRepository.GetAllAsync(cancellationToken);
        var breakdown = new List<EnvironmentStatistics>();

        foreach (var env in environments)
        {
            var envLogs = appliedLogs.Where(l => l.EnvironmentId == env.Id).ToList();
            var successCount = envLogs.Count(l => l.Success);
            var failureCount = envLogs.Count(l => !l.Success);
            var totalCount = successCount + failureCount;

            breakdown.Add(new EnvironmentStatistics
            {
                EnvironmentId = env.Id,
                EnvironmentName = env.DisplayName,
                Provider = env.Provider,
                AppliedMigrations = successCount,
                FailedMigrations = failureCount,
                LastMigrationAt = envLogs.Any() ? envLogs.Max(l => l.Timestamp) : null,
                SuccessRate = totalCount > 0
                    ? Math.Round((double)successCount / totalCount * 100, 2)
                    : 0
            });
        }

        return breakdown;
    }

    private static List<UserActivitySummary> GetTopUsersFromLogs(
        List<Data.Entities.AuditLog> appliedLogs,
        List<Data.Entities.AuditLog> rolledBackLogs,
        int count)
    {
        var userGroups = appliedLogs
            .GroupBy(l => l.UserId)
            .Select(g => new UserActivitySummary
            {
                UserId = g.Key,
                UserEmail = g.First().UserEmail,
                MigrationsApplied = g.Count(),
                MigrationsRolledBack = rolledBackLogs.Count(l => l.UserId == g.Key),
                LastActivityAt = g.Max(l => l.Timestamp)
            })
            .OrderByDescending(u => u.MigrationsApplied)
            .Take(count)
            .ToList();

        return userGroups;
    }

    private static IEnumerable<DateTime> GetDateRange(DateTime from, DateTime to, TrendGranularity granularity)
    {
        var current = granularity switch
        {
            TrendGranularity.Hourly => new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, DateTimeKind.Utc),
            TrendGranularity.Daily => new DateTime(from.Year, from.Month, from.Day, 0, 0, 0, DateTimeKind.Utc),
            TrendGranularity.Weekly => from.AddDays(-(int)from.DayOfWeek).Date,
            TrendGranularity.Monthly => new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => from.Date
        };

        while (current <= to)
        {
            yield return current;
            current = GetNextDate(current, granularity);
        }
    }

    private static DateTime GetNextDate(DateTime date, TrendGranularity granularity)
    {
        return granularity switch
        {
            TrendGranularity.Hourly => date.AddHours(1),
            TrendGranularity.Daily => date.AddDays(1),
            TrendGranularity.Weekly => date.AddDays(7),
            TrendGranularity.Monthly => date.AddMonths(1),
            _ => date.AddDays(1)
        };
    }
}
