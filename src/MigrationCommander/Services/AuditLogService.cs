using System.Text;
using System.Text.Json;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for logging and retrieving audit entries.
/// </summary>
public class AuditLogService : IAuditLogger
{
    private readonly AuditRepository _auditRepository;

    public AuditLogService(AuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    /// <inheritdoc />
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Id == Guid.Empty)
        {
            entry.Id = Guid.NewGuid();
        }

        if (entry.Timestamp == default)
        {
            entry.Timestamp = DateTime.UtcNow;
        }

        var entity = AuditLog.FromDomainModel(entry);
        await _auditRepository.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var entities = await _auditRepository.GetAsync(filter, cancellationToken);
        return entities.Select(e => e.ToDomainModel()).ToList();
    }

    /// <inheritdoc />
    public async Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _auditRepository.GetByIdAsync(id, cancellationToken);
        return entity?.ToDomainModel();
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        return await _auditRepository.GetCountAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportLogsAsync(
        AuditLogFilter filter,
        string format,
        CancellationToken cancellationToken = default)
    {
        // Remove pagination for export
        var exportFilter = new AuditLogFilter
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            EnvironmentId = filter.EnvironmentId,
            Provider = filter.Provider,
            Action = filter.Action,
            UserId = filter.UserId,
            MigrationId = filter.MigrationId,
            SuccessOnly = filter.SuccessOnly,
            Skip = 0,
            Take = int.MaxValue,
            SortBy = filter.SortBy,
            SortDescending = filter.SortDescending
        };

        var logs = await GetLogsAsync(exportFilter, cancellationToken);

        return format.ToLowerInvariant() switch
        {
            "json" => ExportToJson(logs),
            "csv" => ExportToCsv(logs),
            _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
        };
    }

    /// <summary>
    /// Logs a migration applied action.
    /// </summary>
    public async Task LogMigrationAppliedAsync(
        string userId,
        string userEmail,
        string userIpAddress,
        string migrationId,
        DatabaseEnvironment environment,
        bool success,
        TimeSpan duration,
        string? executedSql = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(new AuditLogEntry
        {
            Action = AuditAction.AppliedMigration,
            UserId = userId,
            UserEmail = userEmail,
            UserIpAddress = userIpAddress,
            MigrationId = migrationId,
            EnvironmentId = environment.Id,
            EnvironmentName = environment.DisplayName,
            Provider = environment.Provider,
            Success = success,
            Duration = duration,
            ExecutedSql = executedSql,
            ErrorMessage = errorMessage
        }, cancellationToken);
    }

    /// <summary>
    /// Logs a migration rollback action.
    /// </summary>
    public async Task LogMigrationRolledBackAsync(
        string userId,
        string userEmail,
        string userIpAddress,
        string migrationId,
        DatabaseEnvironment environment,
        bool success,
        TimeSpan duration,
        string? reason = null,
        string? executedSql = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(new AuditLogEntry
        {
            Action = AuditAction.RolledBackMigration,
            UserId = userId,
            UserEmail = userEmail,
            UserIpAddress = userIpAddress,
            MigrationId = migrationId,
            EnvironmentId = environment.Id,
            EnvironmentName = environment.DisplayName,
            Provider = environment.Provider,
            Success = success,
            Duration = duration,
            ExecutedSql = executedSql,
            ErrorMessage = errorMessage,
            Notes = reason
        }, cancellationToken);
    }

    /// <summary>
    /// Logs a SQL preview action.
    /// </summary>
    public async Task LogSqlPreviewedAsync(
        string userId,
        string userEmail,
        string userIpAddress,
        string migrationId,
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(new AuditLogEntry
        {
            Action = AuditAction.PreviewedSql,
            UserId = userId,
            UserEmail = userEmail,
            UserIpAddress = userIpAddress,
            MigrationId = migrationId,
            EnvironmentId = environment.Id,
            EnvironmentName = environment.DisplayName,
            Provider = environment.Provider,
            Success = true,
            Duration = TimeSpan.Zero
        }, cancellationToken);
    }

    private static byte[] ExportToJson(IReadOnlyList<AuditLogEntry> logs)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(logs, options);
        return Encoding.UTF8.GetBytes(json);
    }

    private static byte[] ExportToCsv(IReadOnlyList<AuditLogEntry> logs)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Id,Timestamp,UserId,UserEmail,UserIpAddress,Action,MigrationId,EnvironmentId,EnvironmentName,Provider,Success,DurationMs,ErrorMessage");

        // Data rows
        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.Timestamp:O},{EscapeCsv(log.UserId)},{EscapeCsv(log.UserEmail)},{EscapeCsv(log.UserIpAddress)},{log.Action},{EscapeCsv(log.MigrationId)},{log.EnvironmentId},{EscapeCsv(log.EnvironmentName)},{log.Provider},{log.Success},{log.Duration.TotalMilliseconds:F0},{EscapeCsv(log.ErrorMessage ?? "")}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
