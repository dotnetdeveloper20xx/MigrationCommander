using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Services;

/// <summary>
/// Service for executing migrations against databases.
/// </summary>
public class MigrationExecutorService : IMigrationExecutor
{
    private readonly IMigrationDiscovery _migrationDiscovery;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IMigrationProviderFactory _providerFactory;
    private readonly IAuditLogger _auditLogger;
    private readonly IMigrationNotifier _notifier;

    public event EventHandler<MigrationExecutingEventArgs>? MigrationExecuting;
    public event EventHandler<MigrationExecutedEventArgs>? MigrationExecuted;
    public event EventHandler<MigrationProgressEventArgs>? MigrationProgress;

    public MigrationExecutorService(
        IMigrationDiscovery migrationDiscovery,
        ISqlGenerator sqlGenerator,
        IMigrationProviderFactory providerFactory,
        IAuditLogger auditLogger,
        IMigrationNotifier notifier)
    {
        _migrationDiscovery = migrationDiscovery;
        _sqlGenerator = sqlGenerator;
        _providerFactory = providerFactory;
        _auditLogger = auditLogger;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> ApplyMigrationAsync(
        DatabaseEnvironment environment,
        string migrationId,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MigrationOptions();
        var startedAt = DateTime.UtcNow;

        try
        {
            // Notify migration started
            await _notifier.NotifyMigrationStartedAsync(environment.Id, migrationId);

            // Get migration info
            var migration = await _migrationDiscovery.GetMigrationByIdAsync(migrationId, cancellationToken);
            if (migration == null)
            {
                throw new MigrationNotFoundException(migrationId);
            }

            // Check if already applied
            var appliedMigrations = await _migrationDiscovery.GetAppliedMigrationsAsync(environment, cancellationToken);
            if (appliedMigrations.Any(m => m.Id == migrationId))
            {
                throw new MigrationAlreadyAppliedException(migrationId, environment.Id);
            }

            // Generate SQL
            await ReportProgressAsync(environment.Id, migrationId, 10, "Generating SQL...", MigrationPhase.Preparing);
            var sql = await _sqlGenerator.GenerateUpSqlAsync(migrationId, environment.Provider, cancellationToken);
            migration.UpSql = sql;

            // Fire executing event (allows cancellation)
            await ReportProgressAsync(environment.Id, migrationId, 20, "Validating migration...", MigrationPhase.Validating);
            var executingArgs = new MigrationExecutingEventArgs(migration, environment, sql);
            MigrationExecuting?.Invoke(this, executingArgs);

            if (executingArgs.Cancel)
            {
                throw new MigrationCancelledException(migrationId, executingArgs.CancelReason ?? "Cancelled by event handler");
            }

            // Dry run - just return without executing
            if (options.DryRun)
            {
                var dryRunResult = ExecutionResult.Succeeded(migrationId, environment.Id, environment.Provider, startedAt);
                dryRunResult.ExecutedSql = sql;
                dryRunResult.WasDryRun = true;
                return dryRunResult;
            }

            // Execute the migration
            await ReportProgressAsync(environment.Id, migrationId, 50, "Executing migration...", MigrationPhase.Executing);
            var provider = _providerFactory.GetProvider(environment.Provider);

            cancellationToken.ThrowIfCancellationRequested();

            var rowsAffected = await provider.ExecuteSqlAsync(
                environment.ConnectionString,
                sql,
                options.Timeout,
                cancellationToken);

            // Verify execution
            await ReportProgressAsync(environment.Id, migrationId, 90, "Verifying migration...", MigrationPhase.Verifying);

            var result = ExecutionResult.Succeeded(migrationId, environment.Id, environment.Provider, startedAt);
            result.ExecutedSql = sql;
            result.RowsAffected = rowsAffected;

            // Fire executed event
            await ReportProgressAsync(environment.Id, migrationId, 100, "Migration completed", MigrationPhase.Completed);
            migration.Status = MigrationStatus.Applied;
            migration.AppliedAt = DateTime.UtcNow;
            MigrationExecuted?.Invoke(this, new MigrationExecutedEventArgs(migration, environment, result));

            // Notify completion
            await _notifier.NotifyMigrationCompletedAsync(environment.Id, migrationId, result);

            // Log to audit
            await LogAuditAsync(environment, migrationId, true, result.Duration, sql, null, cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            await ReportProgressAsync(environment.Id, migrationId, 0, "Migration cancelled", MigrationPhase.Failed);
            var cancelledResult = ExecutionResult.Failed(migrationId, environment.Id, environment.Provider, startedAt, "Operation was cancelled");
            await _notifier.NotifyMigrationFailedAsync(environment.Id, migrationId, "Operation was cancelled");
            await LogAuditAsync(environment, migrationId, false, cancelledResult.Duration, null, "Cancelled", cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            await ReportProgressAsync(environment.Id, migrationId, 0, $"Migration failed: {ex.Message}", MigrationPhase.Failed);
            var failedResult = ExecutionResult.Failed(migrationId, environment.Id, environment.Provider, startedAt, ex.Message, ex.StackTrace);
            await _notifier.NotifyMigrationFailedAsync(environment.Id, migrationId, ex.Message);
            await LogAuditAsync(environment, migrationId, false, failedResult.Duration, null, ex.Message, cancellationToken);

            if (ex is MigrationException)
                throw;

            throw new MigrationExecutionException(ex.Message, migrationId, environment.Id, ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutionResult>> ApplyMigrationsAsync(
        DatabaseEnvironment environment,
        IEnumerable<string> migrationIds,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MigrationOptions();
        var results = new List<ExecutionResult>();

        foreach (var migrationId in migrationIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await ApplyMigrationAsync(environment, migrationId, options, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                var failedResult = ExecutionResult.Failed(
                    migrationId,
                    environment.Id,
                    environment.Provider,
                    DateTime.UtcNow,
                    ex.Message,
                    ex.StackTrace);
                results.Add(failedResult);

                if (options.StopOnError)
                {
                    break;
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExecutionResult>> ApplyAllPendingAsync(
        DatabaseEnvironment environment,
        MigrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var pendingMigrations = await _migrationDiscovery.GetPendingMigrationsAsync(environment, cancellationToken);
        var migrationIds = pendingMigrations.Select(m => m.Id).ToList();

        return await ApplyMigrationsAsync(environment, migrationIds, options, cancellationToken);
    }

    private async Task ReportProgressAsync(Guid environmentId, string migrationId, int percentage, string message, MigrationPhase phase)
    {
        MigrationProgress?.Invoke(this, new MigrationProgressEventArgs(migrationId, percentage, message, phase));
        await _notifier.NotifyMigrationProgressAsync(environmentId, migrationId, percentage, message, phase);
    }

    private async Task LogAuditAsync(
        DatabaseEnvironment environment,
        string migrationId,
        bool success,
        TimeSpan duration,
        string? sql,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogger.LogAsync(new AuditLogEntry
            {
                Action = AuditAction.AppliedMigration,
                MigrationId = migrationId,
                EnvironmentId = environment.Id,
                EnvironmentName = environment.DisplayName,
                Provider = environment.Provider,
                Success = success,
                Duration = duration,
                ExecutedSql = sql,
                ErrorMessage = errorMessage,
                UserId = "system",
                UserEmail = "system@migrationcommander",
                UserIpAddress = "127.0.0.1"
            }, cancellationToken);
        }
        catch
        {
            // Don't fail the migration if audit logging fails
        }
    }
}
