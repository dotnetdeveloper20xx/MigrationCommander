using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for scheduling migrations to run at a future time.
/// </summary>
public class MigrationSchedulerService : IMigrationScheduler
{
    private readonly ScheduledMigrationRepository _repository;
    private readonly IEnvironmentManager _environmentManager;
    private readonly IAuditLogger _auditLogger;

    public event EventHandler<ScheduledMigrationExecutingEventArgs>? ScheduledMigrationExecuting;
    public event EventHandler<ScheduledMigrationExecutedEventArgs>? ScheduledMigrationExecuted;

    public MigrationSchedulerService(
        ScheduledMigrationRepository repository,
        IEnvironmentManager environmentManager,
        IAuditLogger auditLogger)
    {
        _repository = repository;
        _environmentManager = environmentManager;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc />
    public async Task<ScheduledMigrationInfo> ScheduleMigrationAsync(
        Guid environmentId,
        string migrationId,
        DateTime scheduledAt,
        string scheduledBy,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        // Validate environment exists
        var environment = await _environmentManager.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            throw new ArgumentException($"Environment with ID {environmentId} not found.", nameof(environmentId));
        }

        // Validate scheduled time is in the future
        if (scheduledAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledAt));
        }

        var entity = new ScheduledMigration
        {
            Id = Guid.NewGuid(),
            MigrationId = migrationId,
            EnvironmentId = environmentId,
            ScheduledAt = scheduledAt,
            ScheduledBy = scheduledBy,
            CreatedAt = DateTime.UtcNow,
            Status = Data.Entities.ScheduledMigrationStatus.Pending,
            Notes = notes
        };

        await _repository.AddAsync(entity, cancellationToken);

        // Log the scheduling action
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = scheduledBy,
            UserEmail = scheduledBy,
            UserIpAddress = "system",
            Action = AuditAction.ScheduledMigration,
            MigrationId = migrationId,
            EnvironmentId = environmentId,
            EnvironmentName = environment.DisplayName,
            Provider = environment.Provider,
            Success = true,
            Duration = TimeSpan.Zero,
            Notes = $"Scheduled for {scheduledAt:yyyy-MM-dd HH:mm:ss} UTC. {notes}"
        }, cancellationToken);

        // Fetch with environment included for proper mapping
        var result = await _repository.GetByIdAsync(entity.Id, cancellationToken);
        return result!.ToDomainModel();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScheduledMigrationInfo>> ScheduleBatchAsync(
        Guid environmentId,
        IEnumerable<string> migrationIds,
        DateTime scheduledAt,
        string scheduledBy,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ScheduledMigrationInfo>();
        var migrationIdList = migrationIds.ToList();

        // Schedule each migration with slightly offset times to maintain order
        for (int i = 0; i < migrationIdList.Count; i++)
        {
            var offsetTime = scheduledAt.AddSeconds(i); // 1 second offset for ordering
            var scheduled = await ScheduleMigrationAsync(
                environmentId,
                migrationIdList[i],
                offsetTime,
                scheduledBy,
                $"Batch {i + 1}/{migrationIdList.Count}. {notes}",
                cancellationToken);
            results.Add(scheduled);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<ScheduledMigrationInfo?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(scheduleId, cancellationToken);
        return entity?.ToDomainModel();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScheduledMigrationInfo>> GetScheduledAsync(
        Guid? environmentId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ScheduledMigration> entities;

        if (environmentId.HasValue)
        {
            entities = await _repository.GetByEnvironmentAsync(environmentId.Value, cancellationToken);
        }
        else
        {
            entities = await _repository.GetPendingAsync(cancellationToken);
        }

        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScheduledMigrationInfo>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetPendingAsync(cancellationToken);
        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScheduledMigrationInfo>> GetDueAsync(
        DateTime? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var checkTime = asOf ?? DateTime.UtcNow;
        var entities = await _repository.GetDueAsync(checkTime, cancellationToken);
        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> CancelScheduleAsync(
        Guid scheduleId,
        string cancelledBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(scheduleId, cancellationToken);
        if (entity == null || entity.Status != Data.Entities.ScheduledMigrationStatus.Pending)
        {
            return false;
        }

        await _repository.CancelAsync(scheduleId, cancelledBy, reason, cancellationToken);

        // Log the cancellation
        var environment = await _environmentManager.GetByIdAsync(entity.EnvironmentId, cancellationToken);
        await _auditLogger.LogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = cancelledBy,
            UserEmail = cancelledBy,
            UserIpAddress = "system",
            Action = AuditAction.CancelledSchedule,
            MigrationId = entity.MigrationId,
            EnvironmentId = entity.EnvironmentId,
            EnvironmentName = environment?.DisplayName ?? "Unknown",
            Provider = environment?.Provider ?? ProviderType.SqlServer,
            Success = true,
            Duration = TimeSpan.Zero,
            Notes = $"Cancelled scheduled migration. Reason: {reason ?? "No reason provided"}"
        }, cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<ScheduledMigrationInfo?> RescheduleAsync(
        Guid scheduleId,
        DateTime newScheduledAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(scheduleId, cancellationToken);
        if (entity == null || entity.Status != Data.Entities.ScheduledMigrationStatus.Pending)
        {
            return null;
        }

        if (newScheduledAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("New scheduled time must be in the future.", nameof(newScheduledAt));
        }

        entity.ScheduledAt = newScheduledAt;
        await _repository.UpdateAsync(entity, cancellationToken);

        return entity.ToDomainModel();
    }

    /// <inheritdoc />
    public async Task MarkAsRunningAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        await _repository.MarkAsRunningAsync(scheduleId, cancellationToken);

        var entity = await _repository.GetByIdAsync(scheduleId, cancellationToken);
        if (entity != null)
        {
            ScheduledMigrationExecuting?.Invoke(this, new ScheduledMigrationExecutingEventArgs
            {
                ScheduleId = scheduleId,
                MigrationId = entity.MigrationId,
                EnvironmentId = entity.EnvironmentId,
                ScheduledAt = entity.ScheduledAt,
                ExecutingAt = DateTime.UtcNow
            });
        }
    }

    /// <inheritdoc />
    public async Task MarkAsCompletedAsync(
        Guid scheduleId,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(scheduleId, cancellationToken);
        var startTime = entity?.ExecutedAt ?? DateTime.UtcNow;

        await _repository.MarkAsCompletedAsync(scheduleId, success, errorMessage, cancellationToken);

        if (entity != null)
        {
            ScheduledMigrationExecuted?.Invoke(this, new ScheduledMigrationExecutedEventArgs
            {
                ScheduleId = scheduleId,
                MigrationId = entity.MigrationId,
                EnvironmentId = entity.EnvironmentId,
                Success = success,
                ErrorMessage = errorMessage,
                Duration = DateTime.UtcNow - startTime,
                CompletedAt = DateTime.UtcNow
            });
        }
    }
}
