using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for logging audit entries.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit entry.
    /// </summary>
    /// <param name="entry">The audit entry to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs based on filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching audit log entries.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific audit log entry by ID.
    /// </summary>
    /// <param name="id">The audit entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit entry, or null if not found.</returns>
    Task<AuditLogEntry?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of audit entries matching the filter.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total count.</returns>
    Task<int> GetCountAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to a specified format.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="format">Export format (e.g., "csv", "json").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exported data as a byte array.</returns>
    Task<byte[]> ExportLogsAsync(
        AuditLogFilter filter,
        string format,
        CancellationToken cancellationToken = default);
}
