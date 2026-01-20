using Microsoft.AspNetCore.SignalR;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Dashboard.Hubs;

/// <summary>
/// SignalR hub for real-time migration updates.
/// </summary>
public class MigrationHub : Hub
{
    /// <summary>
    /// Subscribes the client to updates for a specific environment.
    /// </summary>
    /// <param name="environmentId">The environment ID to subscribe to.</param>
    public async Task SubscribeToEnvironment(Guid environmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"env-{environmentId}");
    }

    /// <summary>
    /// Unsubscribes the client from updates for a specific environment.
    /// </summary>
    /// <param name="environmentId">The environment ID to unsubscribe from.</param>
    public async Task UnsubscribeFromEnvironment(Guid environmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"env-{environmentId}");
    }

    /// <summary>
    /// Subscribes the client to updates for all environments.
    /// </summary>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
    }

    /// <summary>
    /// Unsubscribes the client from updates for all environments.
    /// </summary>
    public async Task UnsubscribeFromAll()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all");
    }
}

/// <summary>
/// Service for sending migration updates to connected clients via SignalR.
/// </summary>
public class SignalRMigrationNotifier : IMigrationNotifier
{
    private readonly IHubContext<MigrationHub> _hubContext;

    public SignalRMigrationNotifier(IHubContext<MigrationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task NotifyMigrationStartedAsync(Guid environmentId, string migrationId)
    {
        var payload = new { EnvironmentId = environmentId, MigrationId = migrationId, StartedAt = DateTime.UtcNow };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("MigrationStarted", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("MigrationStarted", payload);
    }

    /// <inheritdoc />
    public async Task NotifyMigrationProgressAsync(Guid environmentId, string migrationId, int percentage, string message, MigrationPhase phase)
    {
        var payload = new
        {
            EnvironmentId = environmentId,
            MigrationId = migrationId,
            Percentage = percentage,
            Message = message,
            Phase = phase.ToString(),
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("MigrationProgress", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("MigrationProgress", payload);
    }

    /// <inheritdoc />
    public async Task NotifyMigrationCompletedAsync(Guid environmentId, string migrationId, ExecutionResult result)
    {
        var payload = new
        {
            EnvironmentId = environmentId,
            MigrationId = migrationId,
            Success = result.Success,
            Duration = result.Duration.TotalSeconds,
            RowsAffected = result.RowsAffected,
            CompletedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("MigrationCompleted", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("MigrationCompleted", payload);
    }

    /// <inheritdoc />
    public async Task NotifyMigrationFailedAsync(Guid environmentId, string migrationId, string errorMessage)
    {
        var payload = new
        {
            EnvironmentId = environmentId,
            MigrationId = migrationId,
            ErrorMessage = errorMessage,
            FailedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("MigrationFailed", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("MigrationFailed", payload);
    }

    /// <inheritdoc />
    public async Task NotifyRollbackStartedAsync(Guid environmentId, string migrationId)
    {
        var payload = new { EnvironmentId = environmentId, MigrationId = migrationId, StartedAt = DateTime.UtcNow };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("RollbackStarted", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("RollbackStarted", payload);
    }

    /// <inheritdoc />
    public async Task NotifyRollbackCompletedAsync(Guid environmentId, string migrationId, ExecutionResult result)
    {
        var payload = new
        {
            EnvironmentId = environmentId,
            MigrationId = migrationId,
            Success = result.Success,
            Duration = result.Duration.TotalSeconds,
            CompletedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("RollbackCompleted", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("RollbackCompleted", payload);
    }

    /// <inheritdoc />
    public async Task NotifyStatusUpdateAsync(Guid environmentId, string status, string message)
    {
        var payload = new
        {
            EnvironmentId = environmentId,
            Status = status,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"env-{environmentId}")
            .SendAsync("StatusUpdate", payload);

        await _hubContext.Clients.Group("all")
            .SendAsync("StatusUpdate", payload);
    }
}
