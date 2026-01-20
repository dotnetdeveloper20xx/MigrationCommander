using MigrationCommander.Core.Interfaces;
using MigrationCommander.Dashboard.Hubs;
using MigrationCommander.Dashboard.Services;

namespace MigrationCommander.Dashboard.Extensions;

/// <summary>
/// Extension methods for configuring the MigrationCommander Dashboard.
/// </summary>
public static class DashboardExtensions
{
    /// <summary>
    /// Adds the SignalR-based migration notifier to replace the default null notifier.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMigrationCommanderSignalR(this IServiceCollection services)
    {
        // Remove existing NullMigrationNotifier registration and add SignalR notifier
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMigrationNotifier));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddScoped<IMigrationNotifier, SignalRMigrationNotifier>();

        // Add user context service for authentication
        services.AddScoped<UserContextService>();

        return services;
    }

    /// <summary>
    /// Maps the MigrationCommander SignalR hub.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="hubPath">Optional custom path for the hub. Defaults to /hubs/migration.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapMigrationCommanderHub(this IEndpointRouteBuilder endpoints, string hubPath = "/hubs/migration")
    {
        endpoints.MapHub<MigrationHub>(hubPath);
        return endpoints;
    }
}
