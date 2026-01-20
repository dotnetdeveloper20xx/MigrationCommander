using MigrationCommander.Core.Models;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing an audit log entry.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserIpAddress { get; set; } = string.Empty;

    public AuditAction Action { get; set; }
    public string? MigrationId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public string? EnvironmentName { get; set; }
    public ProviderType? Provider { get; set; }

    public bool Success { get; set; }
    public long DurationTicks { get; set; }

    /// <summary>
    /// The SQL that was executed (if applicable).
    /// </summary>
    public string? ExecutedSql { get; set; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional notes or context.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Converts to the domain model.
    /// </summary>
    public AuditLogEntry ToDomainModel()
    {
        return new AuditLogEntry
        {
            Id = Id,
            Timestamp = Timestamp,
            UserId = UserId,
            UserEmail = UserEmail,
            UserIpAddress = UserIpAddress,
            Action = Action,
            MigrationId = MigrationId ?? string.Empty,
            EnvironmentId = EnvironmentId ?? Guid.Empty,
            EnvironmentName = EnvironmentName ?? string.Empty,
            Provider = Provider ?? ProviderType.SqlServer,
            Success = Success,
            Duration = TimeSpan.FromTicks(DurationTicks),
            ExecutedSql = ExecutedSql,
            ErrorMessage = ErrorMessage,
            Notes = Notes
        };
    }

    /// <summary>
    /// Creates from a domain model.
    /// </summary>
    public static AuditLog FromDomainModel(AuditLogEntry entry)
    {
        return new AuditLog
        {
            Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            Timestamp = entry.Timestamp == default ? DateTime.UtcNow : entry.Timestamp,
            UserId = entry.UserId,
            UserEmail = entry.UserEmail,
            UserIpAddress = entry.UserIpAddress,
            Action = entry.Action,
            MigrationId = string.IsNullOrEmpty(entry.MigrationId) ? null : entry.MigrationId,
            EnvironmentId = entry.EnvironmentId == Guid.Empty ? null : entry.EnvironmentId,
            EnvironmentName = string.IsNullOrEmpty(entry.EnvironmentName) ? null : entry.EnvironmentName,
            Provider = entry.Provider,
            Success = entry.Success,
            DurationTicks = entry.Duration.Ticks,
            ExecutedSql = entry.ExecutedSql,
            ErrorMessage = entry.ErrorMessage,
            Notes = entry.Notes
        };
    }
}
