using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Services;
using MigrationCommander.Data;
using MigrationCommander.Data.Repositories;
using MigrationCommander.Providers;
using MigrationCommander.Providers.MySQL;
using MigrationCommander.Providers.PostgreSQL;
using MigrationCommander.Providers.SQLite;
using MigrationCommander.Providers.SqlServer;
using MigrationCommander.Services;

namespace MigrationCommander.Extensions;

/// <summary>
/// Extension methods for configuring MigrationCommander services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MigrationCommander services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMigrationCommander(
        this IServiceCollection services,
        Action<MigrationCommanderOptions> configure)
    {
        var options = new MigrationCommanderOptions();
        configure(options);

        services.AddSingleton(options);

        // Configure Data Protection for encrypting connection strings
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName(options.ApplicationName);

        if (!string.IsNullOrEmpty(options.DataProtectionKeyPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(options.DataProtectionKeyPath));
        }

        // Register internal SQLite database context
        var connectionString = options.InternalDatabasePath ?? "Data Source=migration_commander.db";
        services.AddDbContext<MigrationCommanderDbContext>(opts =>
        {
            opts.UseSqlite(connectionString);
        });

        // Register repositories
        services.AddScoped<DatabaseRepository>();
        services.AddScoped<AuditRepository>();
        services.AddScoped<HistoryRepository>();
        services.AddScoped<ScheduledMigrationRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<ApprovalRequestRepository>();

        // Register providers
        services.AddScoped<SqlServerMigrationProvider>();
        services.AddScoped<PostgreSqlMigrationProvider>();
        services.AddScoped<MySqlMigrationProvider>();
        services.AddScoped<SqliteMigrationProvider>();
        services.AddScoped<IMigrationProviderFactory, MigrationProviderFactory>();

        // Register core services
        services.AddScoped<IMigrationDiscovery, MigrationDiscoveryService>();
        services.AddScoped<ISqlGenerator, SqlPreviewGenerator>();
        services.AddScoped<IDataImpactAnalyzer, DataImpactAnalyzer>();
        services.AddScoped<IAuditLogger, AuditLogService>();
        services.AddScoped<IEnvironmentManager, EnvironmentManager>();
        services.AddScoped<IMigrationExecutor, MigrationExecutorService>();
        services.AddScoped<IRollbackManager, RollbackManager>();

        // Phase 5: Migration Scheduling and Dependencies
        services.AddScoped<IMigrationScheduler, MigrationSchedulerService>();
        services.AddScoped<IDependencyResolver, DependencyResolverService>();

        // Phase 6: Reporting and Statistics
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IReportGenerator, ReportGeneratorService>();

        // Phase 7: Security and Approvals
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IApprovalWorkflow, ApprovalWorkflowService>();

        // Register null notifier by default (can be replaced with SignalR notifier in Dashboard)
        services.AddSingleton<IMigrationNotifier, NullMigrationNotifier>();

        // Add SignalR if enabled
        if (options.EnableRealTimeUpdates)
        {
            services.AddSignalR();
        }

        return services;
    }

    /// <summary>
    /// Configures MigrationCommander middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseMigrationCommander(this IApplicationBuilder app)
    {
        // Ensure internal database is created and seeded
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MigrationCommanderDbContext>();
        db.Database.EnsureCreated();

        // Seed default roles for authorization
        var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
        userRepository.SeedDefaultRolesAsync().GetAwaiter().GetResult();

        return app;
    }

    /// <summary>
    /// Maps MigrationCommander endpoints including SignalR hub.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapMigrationCommander(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<MigrationCommanderOptions>();

        if (options.EnableRealTimeUpdates)
        {
            // Map SignalR hub (to be implemented in Phase 6)
            // endpoints.MapHub<MigrationHub>("/hubs/migration");
        }

        return endpoints;
    }
}
