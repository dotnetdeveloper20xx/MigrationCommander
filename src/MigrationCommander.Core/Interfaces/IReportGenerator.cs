using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Interfaces;

/// <summary>
/// Service for generating reports in various formats.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a migration summary report.
    /// </summary>
    /// <param name="filter">Report filter criteria.</param>
    /// <param name="format">Output format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Report data as byte array.</returns>
    Task<byte[]> GenerateMigrationReportAsync(
        ReportFilter filter,
        ReportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an audit trail report.
    /// </summary>
    /// <param name="filter">Audit log filter criteria.</param>
    /// <param name="format">Output format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Report data as byte array.</returns>
    Task<byte[]> GenerateAuditReportAsync(
        AuditLogFilter filter,
        ReportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an environment status report.
    /// </summary>
    /// <param name="environmentId">The environment ID.</param>
    /// <param name="format">Output format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Report data as byte array.</returns>
    Task<byte[]> GenerateEnvironmentReportAsync(
        Guid environmentId,
        ReportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an environment comparison report.
    /// </summary>
    /// <param name="environmentIds">Environment IDs to compare.</param>
    /// <param name="format">Output format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Report data as byte array.</returns>
    Task<byte[]> GenerateComparisonReportAsync(
        IEnumerable<Guid> environmentIds,
        ReportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file extension for a report format.
    /// </summary>
    /// <param name="format">The report format.</param>
    /// <returns>File extension (e.g., ".pdf", ".xlsx").</returns>
    string GetFileExtension(ReportFormat format);

    /// <summary>
    /// Gets the MIME type for a report format.
    /// </summary>
    /// <param name="format">The report format.</param>
    /// <returns>MIME type string.</returns>
    string GetMimeType(ReportFormat format);
}
