using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Services;

/// <summary>
/// Service for managing migration rollbacks.
/// </summary>
public class RollbackManager : IRollbackManager
{
    private readonly IMigrationDiscovery _migrationDiscovery;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IDataImpactAnalyzer _impactAnalyzer;
    private readonly IMigrationProviderFactory _providerFactory;
    private readonly IAuditLogger _auditLogger;
    private readonly IMigrationNotifier _notifier;

    public RollbackManager(
        IMigrationDiscovery migrationDiscovery,
        ISqlGenerator sqlGenerator,
        IDataImpactAnalyzer impactAnalyzer,
        IMigrationProviderFactory providerFactory,
        IAuditLogger auditLogger,
        IMigrationNotifier notifier)
    {
        _migrationDiscovery = migrationDiscovery;
        _sqlGenerator = sqlGenerator;
        _impactAnalyzer = impactAnalyzer;
        _providerFactory = providerFactory;
        _auditLogger = auditLogger;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<RollbackAnalysis> AnalyzeRollbackAsync(
        DatabaseEnvironment environment,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var analysis = new RollbackAnalysis
        {
            MigrationId = migrationId,
            EnvironmentId = environment.Id
        };

        // Check if migration is applied
        var appliedMigrations = await _migrationDiscovery.GetAppliedMigrationsAsync(environment, cancellationToken);
        var migration = appliedMigrations.FirstOrDefault(m => m.Id == migrationId);

        if (migration == null)
        {
            analysis.CanRollback = false;
            analysis.BlockingReason = "Migration has not been applied to this environment.";
            analysis.RiskLevel = RollbackRiskLevel.Low;
            return analysis;
        }

        // Check for dependent migrations (migrations applied after this one)
        var dependentMigrations = appliedMigrations
            .Where(m => m.Timestamp > migration.Timestamp)
            .Select(m => m.Id)
            .ToList();

        if (dependentMigrations.Any())
        {
            analysis.CanRollback = false;
            analysis.BlockingReason = $"Cannot rollback: {dependentMigrations.Count} migration(s) were applied after this one and must be rolled back first.";
            analysis.DependentMigrations = dependentMigrations;
            analysis.RiskLevel = RollbackRiskLevel.High;
            return analysis;
        }

        // Generate DOWN SQL
        try
        {
            analysis.DownSql = await _sqlGenerator.GenerateDownSqlAsync(migrationId, environment.Provider, cancellationToken);
        }
        catch (Exception ex)
        {
            analysis.CanRollback = false;
            analysis.BlockingReason = $"Cannot generate rollback SQL: {ex.Message}";
            analysis.RiskLevel = RollbackRiskLevel.Critical;
            return analysis;
        }

        // Analyze data impact
        var tableImpacts = await _impactAnalyzer.AnalyzeRollbackImpactAsync(environment, migrationId, cancellationToken);
        analysis.AffectedTables = tableImpacts.ToList();
        analysis.TotalRowsAffected = tableImpacts.Sum(t => t.RowsToBeDeleted);

        // Determine risk level
        analysis.WillDropTables = tableImpacts.Any(t => t.WillDropTable);
        analysis.WillDropColumns = tableImpacts.Any(t => t.Action.Contains("DROP COLUMN", StringComparison.OrdinalIgnoreCase));
        analysis.WillDeleteData = tableImpacts.Any(t => t.RowsToBeDeleted > 0);

        if (analysis.WillDropTables)
        {
            analysis.RiskLevel = RollbackRiskLevel.Critical;
            analysis.Warnings.Add($"This rollback will DROP {tableImpacts.Count(t => t.WillDropTable)} table(s).");
        }
        else if (analysis.WillDeleteData && analysis.TotalRowsAffected > 1000)
        {
            analysis.RiskLevel = RollbackRiskLevel.High;
            analysis.Warnings.Add($"This rollback may affect {analysis.TotalRowsAffected:N0} rows of data.");
        }
        else if (analysis.WillDropColumns)
        {
            analysis.RiskLevel = RollbackRiskLevel.Medium;
            analysis.Warnings.Add("This rollback will drop one or more columns.");
        }
        else
        {
            analysis.RiskLevel = RollbackRiskLevel.Low;
        }

        // Add production warning
        if (environment.IsProduction)
        {
            analysis.Warnings.Insert(0, "WARNING: This is a PRODUCTION environment!");
        }

        // Estimate duration
        analysis.EstimatedDuration = await _impactAnalyzer.EstimateDurationAsync(environment, migrationId, cancellationToken);

        analysis.CanRollback = true;
        return analysis;
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> RollbackMigrationAsync(
        DatabaseEnvironment environment,
        string migrationId,
        RollbackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new RollbackOptions();
        var startedAt = DateTime.UtcNow;

        try
        {
            // Notify rollback started
            await _notifier.NotifyRollbackStartedAsync(environment.Id, migrationId);

            // Analyze first
            var analysis = await AnalyzeRollbackAsync(environment, migrationId, cancellationToken);

            if (!analysis.CanRollback)
            {
                throw new RollbackNotPossibleException(migrationId, environment.Id, analysis.BlockingReason ?? "Unknown reason");
            }

            // Check if force is required for high-risk rollbacks
            if (analysis.RiskLevel >= RollbackRiskLevel.High && !options.Force)
            {
                throw new RollbackNotPossibleException(
                    migrationId,
                    environment.Id,
                    $"Rollback has risk level '{analysis.RiskLevel}'. Use Force option to proceed.");
            }

            // Get the SQL
            var sql = analysis.DownSql;
            if (string.IsNullOrEmpty(sql))
            {
                sql = await _sqlGenerator.GenerateDownSqlAsync(migrationId, environment.Provider, cancellationToken);
            }

            // Execute the rollback
            var provider = _providerFactory.GetProvider(environment.Provider);

            cancellationToken.ThrowIfCancellationRequested();

            var rowsAffected = await provider.ExecuteSqlAsync(
                environment.ConnectionString,
                sql,
                options.Timeout,
                cancellationToken);

            var result = ExecutionResult.Succeeded(migrationId, environment.Id, environment.Provider, startedAt);
            result.ExecutedSql = sql;
            result.RowsAffected = rowsAffected;

            // Notify rollback completed
            await _notifier.NotifyRollbackCompletedAsync(environment.Id, migrationId, result);

            // Log to audit
            await LogRollbackAuditAsync(environment, migrationId, true, result.Duration, sql, options.Reason, null, cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            var cancelledResult = ExecutionResult.Failed(migrationId, environment.Id, environment.Provider, startedAt, "Rollback was cancelled");
            await _notifier.NotifyMigrationFailedAsync(environment.Id, migrationId, "Rollback was cancelled");
            await LogRollbackAuditAsync(environment, migrationId, false, cancelledResult.Duration, null, options?.Reason, "Cancelled", cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            var failedResult = ExecutionResult.Failed(migrationId, environment.Id, environment.Provider, startedAt, ex.Message, ex.StackTrace);
            await _notifier.NotifyMigrationFailedAsync(environment.Id, migrationId, $"Rollback failed: {ex.Message}");
            await LogRollbackAuditAsync(environment, migrationId, false, failedResult.Duration, null, options?.Reason, ex.Message, cancellationToken);

            if (ex is RollbackException)
                throw;

            throw new RollbackException(ex.Message, migrationId, environment.Id, ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutionResult>> RollbackToMigrationAsync(
        DatabaseEnvironment environment,
        string targetMigrationId,
        RollbackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ExecutionResult>();

        // Get all applied migrations
        var appliedMigrations = await _migrationDiscovery.GetAppliedMigrationsAsync(environment, cancellationToken);
        var targetMigration = appliedMigrations.FirstOrDefault(m => m.Id == targetMigrationId);

        if (targetMigration == null)
        {
            throw new MigrationNotAppliedException(targetMigrationId, environment.Id);
        }

        // Get migrations to rollback (all applied after target, in reverse order)
        var migrationsToRollback = appliedMigrations
            .Where(m => m.Timestamp > targetMigration.Timestamp)
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        if (!migrationsToRollback.Any())
        {
            return results; // Nothing to rollback
        }

        // Rollback each migration in reverse order
        foreach (var migration in migrationsToRollback)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await RollbackMigrationAsync(environment, migration.Id, options, cancellationToken);
            results.Add(result);

            if (!result.Success)
            {
                break; // Stop on first failure
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutionResult>> RollbackAllAsync(
        DatabaseEnvironment environment,
        RollbackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ExecutionResult>();

        // Get all applied migrations in reverse order
        var appliedMigrations = await _migrationDiscovery.GetAppliedMigrationsAsync(environment, cancellationToken);
        var migrationsToRollback = appliedMigrations
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        foreach (var migration in migrationsToRollback)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await RollbackMigrationAsync(environment, migration.Id, options, cancellationToken);
            results.Add(result);

            if (!result.Success)
            {
                break;
            }
        }

        return results;
    }

    private async Task LogRollbackAuditAsync(
        DatabaseEnvironment environment,
        string migrationId,
        bool success,
        TimeSpan duration,
        string? sql,
        string? reason,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                Action = AuditAction.RolledBackMigration,
                MigrationId = migrationId,
                EnvironmentId = environment.Id,
                EnvironmentName = environment.DisplayName,
                Provider = environment.Provider,
                Success = success,
                Duration = duration,
                ExecutedSql = sql,
                ErrorMessage = errorMessage,
                Notes = reason,
                UserId = "system",
                UserEmail = "system@migrationcommander",
                UserIpAddress = "127.0.0.1"
            }, cancellationToken);
        }
        catch
        {
            // Don't fail the rollback if audit logging fails
        }
    }
}
