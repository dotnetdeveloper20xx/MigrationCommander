using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Repositories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MigrationCommander.Services;

/// <summary>
/// Service for generating reports in various formats.
/// </summary>
public class ReportGeneratorService : IReportGenerator
{
    private readonly AuditRepository _auditRepository;
    private readonly DatabaseRepository _databaseRepository;
    private readonly HistoryRepository _historyRepository;
    private readonly IStatisticsService _statisticsService;

    static ReportGeneratorService()
    {
        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportGeneratorService(
        AuditRepository auditRepository,
        DatabaseRepository databaseRepository,
        HistoryRepository historyRepository,
        IStatisticsService statisticsService)
    {
        _auditRepository = auditRepository;
        _databaseRepository = databaseRepository;
        _historyRepository = historyRepository;
        _statisticsService = statisticsService;
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateMigrationReportAsync(
        ReportFilter filter,
        ReportFormat format,
        CancellationToken cancellationToken = default)
    {
        var auditFilter = new AuditLogFilter
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            EnvironmentId = filter.EnvironmentIds?.FirstOrDefault(),
            Provider = filter.Provider,
            UserId = filter.UserId,
            SuccessOnly = filter.SuccessOnly,
            Skip = 0,
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(auditFilter, cancellationToken);
        var migrationLogs = logs
            .Where(l => l.Action == AuditAction.AppliedMigration || l.Action == AuditAction.RolledBackMigration)
            .Select(l => l.ToDomainModel())
            .ToList();

        return format switch
        {
            ReportFormat.Json => GenerateMigrationReportJson(migrationLogs),
            ReportFormat.Csv => GenerateMigrationReportCsv(migrationLogs),
            ReportFormat.Html => GenerateMigrationReportHtml(migrationLogs, filter),
            ReportFormat.Pdf => GenerateMigrationReportPdf(migrationLogs, filter),
            ReportFormat.Excel => GenerateMigrationReportExcel(migrationLogs, filter),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateAuditReportAsync(
        AuditLogFilter filter,
        ReportFormat format,
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
            Take = int.MaxValue
        };

        var logs = await _auditRepository.GetAsync(exportFilter, cancellationToken);
        var auditLogs = logs.Select(l => l.ToDomainModel()).ToList();

        return format switch
        {
            ReportFormat.Json => GenerateAuditReportJson(auditLogs),
            ReportFormat.Csv => GenerateAuditReportCsv(auditLogs),
            ReportFormat.Html => GenerateAuditReportHtml(auditLogs),
            ReportFormat.Pdf => GenerateAuditReportPdf(auditLogs),
            ReportFormat.Excel => GenerateAuditReportExcel(auditLogs),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateEnvironmentReportAsync(
        Guid environmentId,
        ReportFormat format,
        CancellationToken cancellationToken = default)
    {
        var environment = await _databaseRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment == null)
        {
            throw new ArgumentException($"Environment not found: {environmentId}", nameof(environmentId));
        }

        var histories = await _historyRepository.GetByEnvironmentAsync(environmentId, cancellationToken);
        var stats = await _statisticsService.GetEnvironmentStatisticsAsync(environmentId, null, null, cancellationToken);

        var report = new EnvironmentReportData
        {
            Environment = environment.ToDomainModel(string.Empty), // Connection string not needed for report
            Statistics = stats,
            MigrationHistory = histories.Select(h => new MigrationHistoryItem
            {
                MigrationId = h.MigrationId,
                Status = h.Status,
                ExecutedAt = h.ExecutedAt,
                ExecutedBy = h.ExecutedBy ?? "Unknown",
                DurationMs = (long)h.Duration.TotalMilliseconds,
                ErrorMessage = h.ErrorMessage
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        return format switch
        {
            ReportFormat.Json => GenerateEnvironmentReportJson(report),
            ReportFormat.Csv => GenerateEnvironmentReportCsv(report),
            ReportFormat.Html => GenerateEnvironmentReportHtml(report),
            ReportFormat.Pdf => GenerateEnvironmentReportPdf(report),
            ReportFormat.Excel => GenerateEnvironmentReportExcel(report),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateComparisonReportAsync(
        IEnumerable<Guid> environmentIds,
        ReportFormat format,
        CancellationToken cancellationToken = default)
    {
        var envList = environmentIds.ToList();
        var comparisonData = new List<EnvironmentComparisonItem>();

        foreach (var envId in envList)
        {
            var environment = await _databaseRepository.GetByIdAsync(envId, cancellationToken);
            if (environment == null) continue;

            var stats = await _statisticsService.GetEnvironmentStatisticsAsync(envId, null, null, cancellationToken);
            var histories = await _historyRepository.GetByEnvironmentAsync(envId, cancellationToken);

            comparisonData.Add(new EnvironmentComparisonItem
            {
                EnvironmentId = envId,
                EnvironmentName = environment.DisplayName,
                Provider = environment.Provider,
                TotalMigrations = histories.Count,
                AppliedMigrations = stats.AppliedMigrations,
                PendingMigrations = stats.PendingMigrations,
                FailedMigrations = stats.FailedMigrations,
                SuccessRate = stats.SuccessRate,
                LastMigrationAt = stats.LastMigrationAt,
                MigrationIds = histories.Select(h => h.MigrationId).Distinct().ToList()
            });
        }

        return format switch
        {
            ReportFormat.Json => GenerateComparisonReportJson(comparisonData),
            ReportFormat.Csv => GenerateComparisonReportCsv(comparisonData),
            ReportFormat.Html => GenerateComparisonReportHtml(comparisonData),
            ReportFormat.Pdf => GenerateComparisonReportPdf(comparisonData),
            ReportFormat.Excel => GenerateComparisonReportExcel(comparisonData),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };
    }

    /// <inheritdoc />
    public string GetFileExtension(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Excel => ".xlsx",
            ReportFormat.Csv => ".csv",
            ReportFormat.Json => ".json",
            ReportFormat.Html => ".html",
            _ => ".txt"
        };
    }

    /// <inheritdoc />
    public string GetMimeType(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.Pdf => "application/pdf",
            ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportFormat.Csv => "text/csv",
            ReportFormat.Json => "application/json",
            ReportFormat.Html => "text/html",
            _ => "text/plain"
        };
    }

    #region JSON Generators

    private static byte[] GenerateMigrationReportJson(List<AuditLogEntry> logs)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            TotalRecords = logs.Count,
            Migrations = logs.Select(l => new
            {
                l.Id,
                l.MigrationId,
                l.EnvironmentName,
                l.Action,
                l.Success,
                l.Timestamp,
                l.UserId,
                l.UserEmail,
                DurationMs = l.Duration.TotalMilliseconds,
                l.ErrorMessage
            })
        };

        return SerializeToJson(report);
    }

    private static byte[] GenerateAuditReportJson(List<AuditLogEntry> logs)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            TotalRecords = logs.Count,
            AuditLogs = logs.Select(l => new
            {
                l.Id,
                l.Timestamp,
                l.UserId,
                l.UserEmail,
                l.UserIpAddress,
                Action = l.Action.ToString(),
                l.MigrationId,
                l.EnvironmentId,
                l.EnvironmentName,
                Provider = l.Provider.ToString(),
                l.Success,
                DurationMs = l.Duration.TotalMilliseconds,
                l.ErrorMessage,
                l.Notes
            })
        };

        return SerializeToJson(report);
    }

    private static byte[] GenerateEnvironmentReportJson(EnvironmentReportData report)
    {
        return SerializeToJson(report);
    }

    private static byte[] GenerateComparisonReportJson(List<EnvironmentComparisonItem> data)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            EnvironmentCount = data.Count,
            Environments = data
        };

        return SerializeToJson(report);
    }

    #endregion

    #region CSV Generators

    private static byte[] GenerateMigrationReportCsv(List<AuditLogEntry> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MigrationId,Environment,Action,Success,Timestamp,UserId,UserEmail,DurationMs,ErrorMessage");

        foreach (var log in logs)
        {
            sb.AppendLine($"{EscapeCsv(log.MigrationId ?? "")},{EscapeCsv(log.EnvironmentName)},{log.Action},{log.Success},{log.Timestamp:O},{EscapeCsv(log.UserId)},{EscapeCsv(log.UserEmail)},{log.Duration.TotalMilliseconds:F0},{EscapeCsv(log.ErrorMessage ?? "")}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateAuditReportCsv(List<AuditLogEntry> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Timestamp,UserId,UserEmail,UserIpAddress,Action,MigrationId,EnvironmentId,EnvironmentName,Provider,Success,DurationMs,ErrorMessage");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.Timestamp:O},{EscapeCsv(log.UserId)},{EscapeCsv(log.UserEmail)},{EscapeCsv(log.UserIpAddress)},{log.Action},{EscapeCsv(log.MigrationId ?? "")},{log.EnvironmentId},{EscapeCsv(log.EnvironmentName)},{log.Provider},{log.Success},{log.Duration.TotalMilliseconds:F0},{EscapeCsv(log.ErrorMessage ?? "")}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateEnvironmentReportCsv(EnvironmentReportData report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("EnvironmentName,Provider,AppliedMigrations,PendingMigrations,FailedMigrations,SuccessRate");
        sb.AppendLine($"{EscapeCsv(report.Environment.DisplayName)},{report.Environment.Provider},{report.Statistics.AppliedMigrations},{report.Statistics.PendingMigrations},{report.Statistics.FailedMigrations},{report.Statistics.SuccessRate}%");

        sb.AppendLine();
        sb.AppendLine("MigrationHistory");
        sb.AppendLine("MigrationId,Status,ExecutedAt,ExecutedBy,DurationMs,ErrorMessage");

        foreach (var item in report.MigrationHistory)
        {
            sb.AppendLine($"{EscapeCsv(item.MigrationId)},{item.Status},{item.ExecutedAt:O},{EscapeCsv(item.ExecutedBy)},{item.DurationMs},{EscapeCsv(item.ErrorMessage ?? "")}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateComparisonReportCsv(List<EnvironmentComparisonItem> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("EnvironmentId,EnvironmentName,Provider,TotalMigrations,AppliedMigrations,PendingMigrations,FailedMigrations,SuccessRate,LastMigrationAt");

        foreach (var item in data)
        {
            sb.AppendLine($"{item.EnvironmentId},{EscapeCsv(item.EnvironmentName)},{item.Provider},{item.TotalMigrations},{item.AppliedMigrations},{item.PendingMigrations},{item.FailedMigrations},{item.SuccessRate}%,{item.LastMigrationAt?.ToString("O") ?? "N/A"}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

    #region HTML Generators

    private static byte[] GenerateMigrationReportHtml(List<AuditLogEntry> logs, ReportFilter filter)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><title>Migration Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background-color:#4CAF50;color:white}.success{color:green}.failure{color:red}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>Migration Report</h1>");
        sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p>Period: {filter.FromDate?.ToString("yyyy-MM-dd") ?? "All time"} to {filter.ToDate?.ToString("yyyy-MM-dd") ?? "Now"}</p>");
        sb.AppendLine($"<p>Total Records: {logs.Count}</p>");
        sb.AppendLine("<table><thead><tr><th>Migration ID</th><th>Environment</th><th>Action</th><th>Status</th><th>Timestamp</th><th>User</th><th>Duration</th></tr></thead><tbody>");

        foreach (var log in logs.Take(1000)) // Limit for HTML
        {
            var statusClass = log.Success ? "success" : "failure";
            sb.AppendLine($"<tr><td>{log.MigrationId}</td><td>{log.EnvironmentName}</td><td>{log.Action}</td><td class=\"{statusClass}\">{(log.Success ? "Success" : "Failed")}</td><td>{log.Timestamp:yyyy-MM-dd HH:mm:ss}</td><td>{log.UserEmail}</td><td>{log.Duration.TotalMilliseconds:F0}ms</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateAuditReportHtml(List<AuditLogEntry> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><title>Audit Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background-color:#2196F3;color:white}.success{color:green}.failure{color:red}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>Audit Trail Report</h1>");
        sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p>Total Records: {logs.Count}</p>");
        sb.AppendLine("<table><thead><tr><th>Timestamp</th><th>User</th><th>IP Address</th><th>Action</th><th>Environment</th><th>Migration</th><th>Status</th></tr></thead><tbody>");

        foreach (var log in logs.Take(1000))
        {
            var statusClass = log.Success ? "success" : "failure";
            sb.AppendLine($"<tr><td>{log.Timestamp:yyyy-MM-dd HH:mm:ss}</td><td>{log.UserEmail}</td><td>{log.UserIpAddress}</td><td>{log.Action}</td><td>{log.EnvironmentName}</td><td>{log.MigrationId}</td><td class=\"{statusClass}\">{(log.Success ? "Success" : "Failed")}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateEnvironmentReportHtml(EnvironmentReportData report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><title>Environment Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background-color:#673AB7;color:white}.stat-card{display:inline-block;padding:20px;margin:10px;border:1px solid #ddd;border-radius:5px}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>Environment Report: {report.Environment.DisplayName}</h1>");
        sb.AppendLine($"<p>Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");

        sb.AppendLine("<div class=\"stats\">");
        sb.AppendLine($"<div class=\"stat-card\"><strong>Applied</strong><br/>{report.Statistics.AppliedMigrations}</div>");
        sb.AppendLine($"<div class=\"stat-card\"><strong>Pending</strong><br/>{report.Statistics.PendingMigrations}</div>");
        sb.AppendLine($"<div class=\"stat-card\"><strong>Failed</strong><br/>{report.Statistics.FailedMigrations}</div>");
        sb.AppendLine($"<div class=\"stat-card\"><strong>Success Rate</strong><br/>{report.Statistics.SuccessRate}%</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>Migration History</h2>");
        sb.AppendLine("<table><thead><tr><th>Migration ID</th><th>Status</th><th>Executed At</th><th>Executed By</th><th>Duration</th></tr></thead><tbody>");

        foreach (var item in report.MigrationHistory.Take(100))
        {
            sb.AppendLine($"<tr><td>{item.MigrationId}</td><td>{item.Status}</td><td>{item.ExecutedAt:yyyy-MM-dd HH:mm:ss}</td><td>{item.ExecutedBy}</td><td>{item.DurationMs}ms</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GenerateComparisonReportHtml(List<EnvironmentComparisonItem> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><title>Environment Comparison Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;margin:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:8px;text-align:left}th{background-color:#009688;color:white}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h1>Environment Comparison Report</h1>");
        sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p>Environments Compared: {data.Count}</p>");
        sb.AppendLine("<table><thead><tr><th>Environment</th><th>Provider</th><th>Total</th><th>Applied</th><th>Pending</th><th>Failed</th><th>Success Rate</th><th>Last Migration</th></tr></thead><tbody>");

        foreach (var item in data)
        {
            sb.AppendLine($"<tr><td>{item.EnvironmentName}</td><td>{item.Provider}</td><td>{item.TotalMigrations}</td><td>{item.AppliedMigrations}</td><td>{item.PendingMigrations}</td><td>{item.FailedMigrations}</td><td>{item.SuccessRate}%</td><td>{item.LastMigrationAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion

    #region PDF Generators (QuestPDF)

    private static byte[] GenerateMigrationReportPdf(List<AuditLogEntry> logs, ReportFilter filter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposePdfHeader(c, "Migration Report"));

                page.Content().Element(c =>
                {
                    c.Column(column =>
                    {
                        column.Spacing(10);

                        // Summary
                        column.Item().Text($"Period: {filter.FromDate?.ToString("yyyy-MM-dd") ?? "All time"} to {filter.ToDate?.ToString("yyyy-MM-dd") ?? "Now"}");
                        column.Item().Text($"Total Records: {logs.Count}");

                        // Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Migration ID
                                columns.RelativeColumn(2); // Environment
                                columns.RelativeColumn(2); // Action
                                columns.RelativeColumn(1); // Status
                                columns.RelativeColumn(2); // Timestamp
                                columns.RelativeColumn(2); // User
                                columns.RelativeColumn(1); // Duration
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Migration ID").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Environment").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Action").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Status").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Timestamp").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("User").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Duration").Bold().FontColor(Colors.White);
                            });

                            // Data rows
                            foreach (var log in logs.Take(500))
                            {
                                var bgColor = logs.IndexOf(log) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                var statusColor = log.Success ? Colors.Green.Darken2 : Colors.Red.Darken2;

                                table.Cell().Background(bgColor).Padding(3).Text(log.MigrationId ?? "");
                                table.Cell().Background(bgColor).Padding(3).Text(log.EnvironmentName);
                                table.Cell().Background(bgColor).Padding(3).Text(log.Action.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(log.Success ? "Success" : "Failed").FontColor(statusColor);
                                table.Cell().Background(bgColor).Padding(3).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm"));
                                table.Cell().Background(bgColor).Padding(3).Text(log.UserEmail);
                                table.Cell().Background(bgColor).Padding(3).Text($"{log.Duration.TotalMilliseconds:F0}ms");
                            }
                        });
                    });
                });

                page.Footer().Element(ComposePdfFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] GenerateAuditReportPdf(List<AuditLogEntry> logs)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposePdfHeader(c, "Audit Trail Report"));

                page.Content().Element(c =>
                {
                    c.Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Text($"Total Records: {logs.Count}");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Timestamp
                                columns.RelativeColumn(2); // User
                                columns.RelativeColumn(2); // Action
                                columns.RelativeColumn(2); // Environment
                                columns.RelativeColumn(2); // Migration
                                columns.RelativeColumn(1); // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Timestamp").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("User").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Action").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Environment").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Migration").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Status").Bold().FontColor(Colors.White);
                            });

                            foreach (var log in logs.Take(500))
                            {
                                var bgColor = logs.IndexOf(log) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                var statusColor = log.Success ? Colors.Green.Darken2 : Colors.Red.Darken2;

                                table.Cell().Background(bgColor).Padding(3).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm"));
                                table.Cell().Background(bgColor).Padding(3).Text(log.UserEmail);
                                table.Cell().Background(bgColor).Padding(3).Text(log.Action.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(log.EnvironmentName);
                                table.Cell().Background(bgColor).Padding(3).Text(log.MigrationId ?? "");
                                table.Cell().Background(bgColor).Padding(3).Text(log.Success ? "Success" : "Failed").FontColor(statusColor);
                            }
                        });
                    });
                });

                page.Footer().Element(ComposePdfFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] GenerateEnvironmentReportPdf(EnvironmentReportData report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposePdfHeader(c, $"Environment Report: {report.Environment.DisplayName}"));

                page.Content().Element(c =>
                {
                    c.Column(column =>
                    {
                        column.Spacing(15);

                        // Statistics cards
                        column.Item().Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem().Element(e => ComposeStatCard(e, "Applied", report.Statistics.AppliedMigrations.ToString(), Colors.Green.Medium));
                            row.RelativeItem().Element(e => ComposeStatCard(e, "Pending", report.Statistics.PendingMigrations.ToString(), Colors.Orange.Medium));
                            row.RelativeItem().Element(e => ComposeStatCard(e, "Failed", report.Statistics.FailedMigrations.ToString(), Colors.Red.Medium));
                            row.RelativeItem().Element(e => ComposeStatCard(e, "Success Rate", $"{report.Statistics.SuccessRate:F1}%", Colors.Blue.Medium));
                        });

                        column.Item().PaddingTop(10).Text("Migration History").FontSize(14).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Migration ID
                                columns.RelativeColumn(1); // Status
                                columns.RelativeColumn(2); // Executed At
                                columns.RelativeColumn(2); // Executed By
                                columns.RelativeColumn(1); // Duration
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.DeepPurple.Medium).Padding(5).Text("Migration ID").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.DeepPurple.Medium).Padding(5).Text("Status").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.DeepPurple.Medium).Padding(5).Text("Executed At").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.DeepPurple.Medium).Padding(5).Text("Executed By").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.DeepPurple.Medium).Padding(5).Text("Duration").Bold().FontColor(Colors.White);
                            });

                            var index = 0;
                            foreach (var item in report.MigrationHistory.Take(100))
                            {
                                var bgColor = index++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bgColor).Padding(3).Text(item.MigrationId);
                                table.Cell().Background(bgColor).Padding(3).Text(item.Status.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(item.ExecutedAt.ToString("yyyy-MM-dd HH:mm"));
                                table.Cell().Background(bgColor).Padding(3).Text(item.ExecutedBy);
                                table.Cell().Background(bgColor).Padding(3).Text($"{item.DurationMs}ms");
                            }
                        });
                    });
                });

                page.Footer().Element(ComposePdfFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] GenerateComparisonReportPdf(List<EnvironmentComparisonItem> data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposePdfHeader(c, "Environment Comparison Report"));

                page.Content().Element(c =>
                {
                    c.Column(column =>
                    {
                        column.Spacing(10);
                        column.Item().Text($"Environments Compared: {data.Count}");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Environment
                                columns.RelativeColumn(1); // Provider
                                columns.RelativeColumn(1); // Total
                                columns.RelativeColumn(1); // Applied
                                columns.RelativeColumn(1); // Pending
                                columns.RelativeColumn(1); // Failed
                                columns.RelativeColumn(1); // Success Rate
                                columns.RelativeColumn(2); // Last Migration
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Environment").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Provider").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Total").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Applied").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Pending").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Failed").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Success %").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Medium).Padding(5).Text("Last Migration").Bold().FontColor(Colors.White);
                            });

                            var index = 0;
                            foreach (var item in data)
                            {
                                var bgColor = index++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bgColor).Padding(3).Text(item.EnvironmentName);
                                table.Cell().Background(bgColor).Padding(3).Text(item.Provider.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(item.TotalMigrations.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(item.AppliedMigrations.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(item.PendingMigrations.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text(item.FailedMigrations.ToString());
                                table.Cell().Background(bgColor).Padding(3).Text($"{item.SuccessRate:F1}%");
                                table.Cell().Background(bgColor).Padding(3).Text(item.LastMigrationAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A");
                            }
                        });
                    });
                });

                page.Footer().Element(ComposePdfFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposePdfHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(title).FontSize(20).Bold().FontColor(Colors.Grey.Darken3);
                column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(100).AlignRight().Text("MigrationCommander").FontSize(10).FontColor(Colors.Grey.Medium);
        });
    }

    private static void ComposePdfFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private static void ComposeStatCard(IContainer container, string label, string value, string color)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().AlignCenter().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1);
            column.Item().AlignCenter().Text(value).FontSize(18).Bold().FontColor(color);
        });
    }

    #endregion

    #region Excel Generators (ClosedXML)

    private static byte[] GenerateMigrationReportExcel(List<AuditLogEntry> logs, ReportFilter filter)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Migrations");

        // Title
        worksheet.Cell(1, 1).Value = "Migration Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 7).Merge();

        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        worksheet.Cell(3, 1).Value = $"Period: {filter.FromDate?.ToString("yyyy-MM-dd") ?? "All time"} to {filter.ToDate?.ToString("yyyy-MM-dd") ?? "Now"}";
        worksheet.Cell(4, 1).Value = $"Total Records: {logs.Count}";

        // Headers
        var headerRow = 6;
        var headers = new[] { "Migration ID", "Environment", "Action", "Status", "Timestamp", "User", "Duration (ms)" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.ForestGreen;
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data
        var row = headerRow + 1;
        foreach (var log in logs)
        {
            worksheet.Cell(row, 1).Value = log.MigrationId ?? "";
            worksheet.Cell(row, 2).Value = log.EnvironmentName;
            worksheet.Cell(row, 3).Value = log.Action.ToString();
            worksheet.Cell(row, 4).Value = log.Success ? "Success" : "Failed";
            worksheet.Cell(row, 4).Style.Font.FontColor = log.Success ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 5).Value = log.Timestamp;
            worksheet.Cell(row, 6).Value = log.UserEmail;
            worksheet.Cell(row, 7).Value = log.Duration.TotalMilliseconds;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add table formatting
        var tableRange = worksheet.Range(headerRow, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] GenerateAuditReportExcel(List<AuditLogEntry> logs)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Log");

        worksheet.Cell(1, 1).Value = "Audit Trail Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 7).Merge();

        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        worksheet.Cell(3, 1).Value = $"Total Records: {logs.Count}";

        var headerRow = 5;
        var headers = new[] { "Timestamp", "User", "IP Address", "Action", "Environment", "Migration", "Status" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.RoyalBlue;
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = headerRow + 1;
        foreach (var log in logs)
        {
            worksheet.Cell(row, 1).Value = log.Timestamp;
            worksheet.Cell(row, 2).Value = log.UserEmail;
            worksheet.Cell(row, 3).Value = log.UserIpAddress;
            worksheet.Cell(row, 4).Value = log.Action.ToString();
            worksheet.Cell(row, 5).Value = log.EnvironmentName;
            worksheet.Cell(row, 6).Value = log.MigrationId ?? "";
            worksheet.Cell(row, 7).Value = log.Success ? "Success" : "Failed";
            worksheet.Cell(row, 7).Style.Font.FontColor = log.Success ? XLColor.Green : XLColor.Red;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        var tableRange = worksheet.Range(headerRow, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] GenerateEnvironmentReportExcel(EnvironmentReportData report)
    {
        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = $"Environment Report: {report.Environment.DisplayName}";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 16;
        summarySheet.Range(1, 1, 1, 4).Merge();

        summarySheet.Cell(2, 1).Value = $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC";
        summarySheet.Cell(3, 1).Value = $"Provider: {report.Environment.Provider}";

        summarySheet.Cell(5, 1).Value = "Statistics";
        summarySheet.Cell(5, 1).Style.Font.Bold = true;

        summarySheet.Cell(6, 1).Value = "Applied Migrations";
        summarySheet.Cell(6, 2).Value = report.Statistics.AppliedMigrations;
        summarySheet.Cell(7, 1).Value = "Pending Migrations";
        summarySheet.Cell(7, 2).Value = report.Statistics.PendingMigrations;
        summarySheet.Cell(8, 1).Value = "Failed Migrations";
        summarySheet.Cell(8, 2).Value = report.Statistics.FailedMigrations;
        summarySheet.Cell(9, 1).Value = "Success Rate";
        summarySheet.Cell(9, 2).Value = $"{report.Statistics.SuccessRate:F1}%";

        summarySheet.Columns().AdjustToContents();

        // History sheet
        var historySheet = workbook.Worksheets.Add("Migration History");
        var headers = new[] { "Migration ID", "Status", "Executed At", "Executed By", "Duration (ms)", "Error" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = historySheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkViolet;
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var item in report.MigrationHistory)
        {
            historySheet.Cell(row, 1).Value = item.MigrationId;
            historySheet.Cell(row, 2).Value = item.Status.ToString();
            historySheet.Cell(row, 3).Value = item.ExecutedAt;
            historySheet.Cell(row, 4).Value = item.ExecutedBy;
            historySheet.Cell(row, 5).Value = item.DurationMs;
            historySheet.Cell(row, 6).Value = item.ErrorMessage ?? "";
            row++;
        }

        historySheet.Columns().AdjustToContents();

        var tableRange = historySheet.Range(1, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] GenerateComparisonReportExcel(List<EnvironmentComparisonItem> data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Comparison");

        worksheet.Cell(1, 1).Value = "Environment Comparison Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 8).Merge();

        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        worksheet.Cell(3, 1).Value = $"Environments Compared: {data.Count}";

        var headerRow = 5;
        var headers = new[] { "Environment", "Provider", "Total", "Applied", "Pending", "Failed", "Success Rate", "Last Migration" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.Teal;
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = headerRow + 1;
        foreach (var item in data)
        {
            worksheet.Cell(row, 1).Value = item.EnvironmentName;
            worksheet.Cell(row, 2).Value = item.Provider.ToString();
            worksheet.Cell(row, 3).Value = item.TotalMigrations;
            worksheet.Cell(row, 4).Value = item.AppliedMigrations;
            worksheet.Cell(row, 5).Value = item.PendingMigrations;
            worksheet.Cell(row, 6).Value = item.FailedMigrations;
            worksheet.Cell(row, 7).Value = $"{item.SuccessRate:F1}%";
            worksheet.Cell(row, 8).Value = item.LastMigrationAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        var tableRange = worksheet.Range(headerRow, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Helpers

    private static byte[] SerializeToJson(object data)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(data, options);
        return Encoding.UTF8.GetBytes(json);
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

    #endregion

    #region Internal Report Models

    private class EnvironmentReportData
    {
        public DatabaseEnvironment Environment { get; set; } = null!;
        public EnvironmentStatistics Statistics { get; set; } = null!;
        public List<MigrationHistoryItem> MigrationHistory { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    private class MigrationHistoryItem
    {
        public string MigrationId { get; set; } = string.Empty;
        public MigrationStatus Status { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string ExecutedBy { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class EnvironmentComparisonItem
    {
        public Guid EnvironmentId { get; set; }
        public string EnvironmentName { get; set; } = string.Empty;
        public ProviderType Provider { get; set; }
        public int TotalMigrations { get; set; }
        public int AppliedMigrations { get; set; }
        public int PendingMigrations { get; set; }
        public int FailedMigrations { get; set; }
        public double SuccessRate { get; set; }
        public DateTime? LastMigrationAt { get; set; }
        public List<string> MigrationIds { get; set; } = new();
    }

    #endregion
}
