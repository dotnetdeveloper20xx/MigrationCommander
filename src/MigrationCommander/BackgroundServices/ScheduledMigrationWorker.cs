using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.BackgroundServices;

/// <summary>
/// Background service that polls for due scheduled migrations and executes them.
/// </summary>
public class ScheduledMigrationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledMigrationWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    public ScheduledMigrationWorker(
        IServiceProvider serviceProvider,
        ILogger<ScheduledMigrationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled Migration Worker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueMigrationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled migrations.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Scheduled Migration Worker stopping.");
    }

    private async Task ProcessDueMigrationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IMigrationScheduler>();
        var executor = scope.ServiceProvider.GetRequiredService<IMigrationExecutor>();
        var environmentManager = scope.ServiceProvider.GetRequiredService<IEnvironmentManager>();
        var notifier = scope.ServiceProvider.GetRequiredService<IMigrationNotifier>();

        var dueMigrations = await scheduler.GetDueAsync(cancellationToken: cancellationToken);

        foreach (var scheduled in dueMigrations)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ExecuteScheduledMigrationAsync(
                scheduled,
                scheduler,
                executor,
                environmentManager,
                notifier,
                cancellationToken);
        }
    }

    private async Task ExecuteScheduledMigrationAsync(
        ScheduledMigrationInfo scheduled,
        IMigrationScheduler scheduler,
        IMigrationExecutor executor,
        IEnvironmentManager environmentManager,
        IMigrationNotifier notifier,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing scheduled migration {MigrationId} for environment {EnvironmentId}",
            scheduled.MigrationId,
            scheduled.EnvironmentId);

        try
        {
            // Mark as running
            await scheduler.MarkAsRunningAsync(scheduled.Id, cancellationToken);

            // Get environment
            var environment = await environmentManager.GetByIdAsync(scheduled.EnvironmentId, cancellationToken);
            if (environment == null)
            {
                var error = $"Environment {scheduled.EnvironmentId} not found.";
                _logger.LogError(error);
                await scheduler.MarkAsCompletedAsync(scheduled.Id, false, error, cancellationToken);
                return;
            }

            // Notify start
            await notifier.NotifyStatusUpdateAsync(
                scheduled.EnvironmentId,
                "Scheduled Execution",
                $"Starting scheduled migration {scheduled.MigrationId}");

            // Execute the migration
            var options = new MigrationOptions
            {
                Notes = $"Scheduled execution. Originally scheduled for {scheduled.ScheduledAt:yyyy-MM-dd HH:mm:ss} UTC"
            };

            var result = await executor.ApplyMigrationAsync(
                environment,
                scheduled.MigrationId,
                options,
                cancellationToken);

            // Mark as completed
            await scheduler.MarkAsCompletedAsync(
                scheduled.Id,
                result.Success,
                result.ErrorMessage,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Scheduled migration {MigrationId} completed successfully.",
                    scheduled.MigrationId);

                await notifier.NotifyStatusUpdateAsync(
                    scheduled.EnvironmentId,
                    "Scheduled Execution Complete",
                    $"Migration {scheduled.MigrationId} completed successfully.");
            }
            else
            {
                _logger.LogWarning(
                    "Scheduled migration {MigrationId} failed: {Error}",
                    scheduled.MigrationId,
                    result.ErrorMessage);

                await notifier.NotifyStatusUpdateAsync(
                    scheduled.EnvironmentId,
                    "Scheduled Execution Failed",
                    $"Migration {scheduled.MigrationId} failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing scheduled migration {MigrationId}",
                scheduled.MigrationId);

            await scheduler.MarkAsCompletedAsync(
                scheduled.Id,
                false,
                ex.Message,
                cancellationToken);

            await notifier.NotifyMigrationFailedAsync(
                scheduled.EnvironmentId,
                scheduled.MigrationId,
                ex.Message);
        }
    }
}
