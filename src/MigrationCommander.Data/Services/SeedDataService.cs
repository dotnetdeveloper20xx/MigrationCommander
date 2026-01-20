using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Core.Models.Security;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data.Services;

/// <summary>
/// Service for seeding comprehensive test data.
/// </summary>
public class SeedDataService
{
    private readonly MigrationCommanderDbContext _context;
    private static readonly Random _random = new(42); // Fixed seed for reproducibility

    // Fixed GUIDs for consistent references
    private static readonly Guid DevEnvId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid QaEnvId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StagingEnvId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProdEnvId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ProdEuEnvId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ProdAsiaEnvId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DbaUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Dev1UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Dev2UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Dev3UserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ViewerUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    public SeedDataService(MigrationCommanderDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seeds all test data if not already seeded.
    /// </summary>
    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        // Check if already seeded by looking for environments
        if (await _context.ConfiguredDatabases.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        await SeedEnvironmentsAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedMigrationHistoryAsync(cancellationToken);
        await SeedAuditLogsAsync(cancellationToken);
        await SeedScheduledMigrationsAsync(cancellationToken);
        await SeedApprovalRequestsAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedEnvironmentsAsync(CancellationToken cancellationToken)
    {
        var environments = new List<ConfiguredDatabase>
        {
            new()
            {
                Id = DevEnvId,
                Name = "development",
                DisplayName = "Development",
                Provider = ProviderType.SqlServer,
                ConnectionStringEncrypted = "encrypted:Server=dev-sql;Database=AppDb;",
                IsProduction = false,
                RequiresApproval = false,
                IsActive = true,
                Color = "#28a745",
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                CreatedBy = "admin"
            },
            new()
            {
                Id = QaEnvId,
                Name = "qa",
                DisplayName = "QA Environment",
                Provider = ProviderType.SqlServer,
                ConnectionStringEncrypted = "encrypted:Server=qa-sql;Database=AppDb;",
                IsProduction = false,
                RequiresApproval = false,
                IsActive = true,
                Color = "#17a2b8",
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                CreatedBy = "admin"
            },
            new()
            {
                Id = StagingEnvId,
                Name = "staging",
                DisplayName = "Staging",
                Provider = ProviderType.SqlServer,
                ConnectionStringEncrypted = "encrypted:Server=staging-sql;Database=AppDb;",
                IsProduction = false,
                RequiresApproval = true,
                IsActive = true,
                Color = "#ffc107",
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                CreatedBy = "admin"
            },
            new()
            {
                Id = ProdEnvId,
                Name = "production-us",
                DisplayName = "Production (US)",
                Provider = ProviderType.SqlServer,
                ConnectionStringEncrypted = "encrypted:Server=prod-us-sql;Database=AppDb;",
                IsProduction = true,
                RequiresApproval = true,
                IsActive = true,
                Color = "#dc3545",
                SortOrder = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                CreatedBy = "admin"
            },
            new()
            {
                Id = ProdEuEnvId,
                Name = "production-eu",
                DisplayName = "Production (EU)",
                Provider = ProviderType.PostgreSQL,
                ConnectionStringEncrypted = "encrypted:Host=prod-eu-pg;Database=appdb;",
                IsProduction = true,
                RequiresApproval = true,
                IsActive = true,
                Color = "#dc3545",
                SortOrder = 5,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                CreatedBy = "admin"
            },
            new()
            {
                Id = ProdAsiaEnvId,
                Name = "production-asia",
                DisplayName = "Production (Asia)",
                Provider = ProviderType.MySQL,
                ConnectionStringEncrypted = "encrypted:Server=prod-asia-mysql;Database=appdb;",
                IsProduction = true,
                RequiresApproval = true,
                IsActive = true,
                Color = "#dc3545",
                SortOrder = 6,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "admin"
            }
        };

        await _context.ConfiguredDatabases.AddRangeAsync(environments, cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        // Get role IDs
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
        var dbaRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "DBA", cancellationToken);
        var devRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Developer", cancellationToken);
        var viewerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Viewer", cancellationToken);

        if (adminRole == null || dbaRole == null || devRole == null || viewerRole == null)
        {
            return; // Roles not seeded yet
        }

        var users = new List<UserEntity>
        {
            new()
            {
                Id = AdminUserId,
                Username = "admin",
                Email = "admin@company.com",
                DisplayName = "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                LastLoginAt = DateTime.UtcNow.AddHours(-1),
                Notes = "Primary system administrator"
            },
            new()
            {
                Id = DbaUserId,
                Username = "sarah.dba",
                Email = "sarah.johnson@company.com",
                DisplayName = "Sarah Johnson",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-85),
                LastLoginAt = DateTime.UtcNow.AddHours(-3),
                Notes = "Senior DBA - Production access"
            },
            new()
            {
                Id = Dev1UserId,
                Username = "john.dev",
                Email = "john.smith@company.com",
                DisplayName = "John Smith",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                LastLoginAt = DateTime.UtcNow.AddMinutes(-30),
                Notes = "Backend developer - Team Alpha"
            },
            new()
            {
                Id = Dev2UserId,
                Username = "emily.dev",
                Email = "emily.chen@company.com",
                DisplayName = "Emily Chen",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                LastLoginAt = DateTime.UtcNow.AddHours(-2),
                Notes = "Full stack developer - Team Beta"
            },
            new()
            {
                Id = Dev3UserId,
                Username = "mike.dev",
                Email = "mike.wilson@company.com",
                DisplayName = "Mike Wilson",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow.AddDays(-2),
                Notes = "Junior developer - Team Alpha"
            },
            new()
            {
                Id = ViewerUserId,
                Username = "viewer",
                Email = "viewer@company.com",
                DisplayName = "Read Only User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                LastLoginAt = DateTime.UtcNow.AddDays(-5),
                Notes = "Audit reviewer - read-only access"
            }
        };

        await _context.Users.AddRangeAsync(users, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Assign roles
        var userRoles = new List<UserRoleEntity>
        {
            new() { UserId = AdminUserId, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow.AddDays(-90), AssignedBy = "system" },
            new() { UserId = DbaUserId, RoleId = dbaRole.Id, AssignedAt = DateTime.UtcNow.AddDays(-85), AssignedBy = "admin" },
            new() { UserId = Dev1UserId, RoleId = devRole.Id, AssignedAt = DateTime.UtcNow.AddDays(-60), AssignedBy = "admin" },
            new() { UserId = Dev2UserId, RoleId = devRole.Id, AssignedAt = DateTime.UtcNow.AddDays(-45), AssignedBy = "admin" },
            new() { UserId = Dev3UserId, RoleId = devRole.Id, AssignedAt = DateTime.UtcNow.AddDays(-30), AssignedBy = "admin" },
            new() { UserId = ViewerUserId, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow.AddDays(-20), AssignedBy = "admin" }
        };

        await _context.UserRoles.AddRangeAsync(userRoles, cancellationToken);
    }

    private async Task SeedMigrationHistoryAsync(CancellationToken cancellationToken)
    {
        var migrations = new List<string>
        {
            "20240101_InitialCreate",
            "20240115_AddUserTable",
            "20240120_AddOrdersTable",
            "20240125_AddProductsTable",
            "20240201_AddCustomerTable",
            "20240210_AddPaymentsTable",
            "20240215_AddShippingTable",
            "20240220_AddInventoryTable",
            "20240301_AddNotificationsTable",
            "20240310_AddAuditLogsTable",
            "20240315_AddUserPreferences",
            "20240320_AddOrderStatusIndex",
            "20240325_AddProductCategoryFk",
            "20240401_AddCustomerAddressTable",
            "20240410_AddPaymentMethodsTable",
            "20240415_AddShippingCarriersTable",
            "20240420_AddDiscountsTable",
            "20240501_AddPromotionsTable",
            "20240510_AddReviewsTable",
            "20240515_AddWishlistTable",
            "20240520_AddCartItemsTable",
            "20240601_AddSearchIndexes",
            "20240610_AddReportingViews",
            "20240615_AddAnalyticsTable",
            "20240620_OptimizeQueries",
            "20240701_AddTwoFactorAuth",
            "20240710_AddApiKeysTable",
            "20240715_AddRateLimiting",
            "20240720_AddCachingLayer",
            "20240801_AddMultiTenancy"
        };

        var environments = new[] { DevEnvId, QaEnvId, StagingEnvId, ProdEnvId, ProdEuEnvId };
        var users = new[] { "admin", "sarah.dba", "john.dev", "emily.dev" };
        var histories = new List<MigrationHistory>();

        // Add migration history for each environment (with varying completeness)
        foreach (var envId in environments)
        {
            var migrationsToApply = envId == ProdEnvId || envId == ProdEuEnvId
                ? migrations.Take(25).ToList() // Production is slightly behind
                : envId == StagingEnvId
                    ? migrations.Take(28).ToList() // Staging is almost current
                    : migrations; // Dev/QA have all migrations

            for (int i = 0; i < migrationsToApply.Count; i++)
            {
                var executedAt = DateTime.UtcNow.AddDays(-90).AddDays(i * 3);
                var user = users[_random.Next(users.Length)];
                var duration = TimeSpan.FromMilliseconds(_random.Next(100, 5000));

                histories.Add(new MigrationHistory
                {
                    Id = Guid.NewGuid(),
                    MigrationId = migrationsToApply[i],
                    EnvironmentId = envId,
                    Provider = envId == ProdEuEnvId ? ProviderType.PostgreSQL :
                              envId == ProdAsiaEnvId ? ProviderType.MySQL : ProviderType.SqlServer,
                    Status = MigrationStatus.Applied,
                    ExecutedAt = executedAt,
                    ExecutedBy = user,
                    Duration = duration,
                    RowsAffected = _random.Next(0, 1000)
                });
            }
        }

        // Add a few failed migrations for realism
        histories.Add(new MigrationHistory
        {
            Id = Guid.NewGuid(),
            MigrationId = "20240805_FailedMigration",
            EnvironmentId = DevEnvId,
            Provider = ProviderType.SqlServer,
            Status = MigrationStatus.Failed,
            ExecutedAt = DateTime.UtcNow.AddDays(-5),
            ExecutedBy = "mike.dev",
            Duration = TimeSpan.FromMilliseconds(1234),
            RowsAffected = 0,
            ErrorMessage = "Cannot drop table 'Products' because it is being referenced by a FOREIGN KEY constraint."
        });

        histories.Add(new MigrationHistory
        {
            Id = Guid.NewGuid(),
            MigrationId = "20240803_RollbackTest",
            EnvironmentId = QaEnvId,
            Provider = ProviderType.SqlServer,
            Status = MigrationStatus.RolledBack,
            ExecutedAt = DateTime.UtcNow.AddDays(-3),
            ExecutedBy = "john.dev",
            Duration = TimeSpan.FromMilliseconds(890),
            RowsAffected = 50,
            Notes = "Rolled back due to performance issues"
        });

        await _context.MigrationHistories.AddRangeAsync(histories, cancellationToken);
    }

    private async Task SeedAuditLogsAsync(CancellationToken cancellationToken)
    {
        var logs = new List<AuditLog>();
        var environments = new (Guid id, string name, ProviderType provider)[]
        {
            (DevEnvId, "Development", ProviderType.SqlServer),
            (QaEnvId, "QA Environment", ProviderType.SqlServer),
            (StagingEnvId, "Staging", ProviderType.SqlServer),
            (ProdEnvId, "Production (US)", ProviderType.SqlServer),
            (ProdEuEnvId, "Production (EU)", ProviderType.PostgreSQL)
        };

        var users = new (string id, string email)[]
        {
            ("admin", "admin@company.com"),
            ("sarah.dba", "sarah.johnson@company.com"),
            ("john.dev", "john.smith@company.com"),
            ("emily.dev", "emily.chen@company.com"),
            ("mike.dev", "mike.wilson@company.com")
        };

        var migrations = new[]
        {
            "20240701_AddTwoFactorAuth", "20240710_AddApiKeysTable", "20240715_AddRateLimiting",
            "20240720_AddCachingLayer", "20240801_AddMultiTenancy", "20240805_FailedMigration"
        };

        // Generate 200+ audit log entries over the past 30 days
        for (int day = 30; day >= 0; day--)
        {
            var entriesPerDay = _random.Next(5, 15);
            for (int i = 0; i < entriesPerDay; i++)
            {
                var user = users[_random.Next(users.Length)];
                var env = environments[_random.Next(environments.Length)];
                var migration = migrations[_random.Next(migrations.Length)];
                var action = GetRandomAction();
                var success = _random.Next(100) > 5; // 95% success rate
                var isLoginAction = action == AuditAction.LoginSuccess || action == AuditAction.LoginFailed;

                logs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow.AddDays(-day).AddHours(_random.Next(8, 18)).AddMinutes(_random.Next(60)),
                    UserId = user.id,
                    UserEmail = user.email,
                    UserIpAddress = $"192.168.1.{_random.Next(1, 255)}",
                    Action = action,
                    MigrationId = isLoginAction ? null : migration,
                    EnvironmentId = isLoginAction ? null : env.id,
                    EnvironmentName = isLoginAction ? null : env.name,
                    Provider = isLoginAction ? null : env.provider,
                    Success = success,
                    DurationTicks = TimeSpan.FromMilliseconds(_random.Next(50, 3000)).Ticks,
                    ErrorMessage = success ? null : "Operation timed out after 30 seconds",
                    Notes = GetNotesForAction(action)
                });
            }
        }

        // Add some specific important events
        logs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow.AddHours(-2),
            UserId = "sarah.dba",
            UserEmail = "sarah.johnson@company.com",
            UserIpAddress = "192.168.1.100",
            Action = AuditAction.ApprovalGranted,
            MigrationId = "20240801_AddMultiTenancy",
            EnvironmentId = ProdEnvId,
            EnvironmentName = "Production (US)",
            Provider = ProviderType.SqlServer,
            Success = true,
            DurationTicks = TimeSpan.FromMilliseconds(150).Ticks,
            Notes = "Approved for production deployment"
        });

        logs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow.AddHours(-1),
            UserId = "john.dev",
            UserEmail = "john.smith@company.com",
            UserIpAddress = "192.168.1.105",
            Action = AuditAction.AppliedMigration,
            MigrationId = "20240801_AddMultiTenancy",
            EnvironmentId = DevEnvId,
            EnvironmentName = "Development",
            Provider = ProviderType.SqlServer,
            Success = true,
            DurationTicks = TimeSpan.FromMilliseconds(2500).Ticks,
            ExecutedSql = "ALTER TABLE Users ADD TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';",
            Notes = "Multi-tenancy support added"
        });

        await _context.AuditLogs.AddRangeAsync(logs, cancellationToken);
    }

    private async Task SeedScheduledMigrationsAsync(CancellationToken cancellationToken)
    {
        var scheduled = new List<ScheduledMigration>
        {
            // Pending scheduled migrations
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240815_AddNewFeature",
                EnvironmentId = ProdEnvId,
                ScheduledAt = DateTime.UtcNow.AddHours(2),
                ScheduledBy = "sarah.dba",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = Entities.ScheduledMigrationStatus.Pending,
                Notes = "Scheduled for off-peak hours deployment"
            },
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240816_PerformanceOptimization",
                EnvironmentId = ProdEuEnvId,
                ScheduledAt = DateTime.UtcNow.AddDays(1).AddHours(3),
                ScheduledBy = "sarah.dba",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Status = Entities.ScheduledMigrationStatus.Pending,
                Notes = "EU maintenance window - 3 AM UTC"
            },
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240817_SecurityPatch",
                EnvironmentId = StagingEnvId,
                ScheduledAt = DateTime.UtcNow.AddHours(6),
                ScheduledBy = "john.dev",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                Status = Entities.ScheduledMigrationStatus.Pending,
                Notes = "Security update for staging validation"
            },
            // Completed scheduled migrations
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240801_AddMultiTenancy",
                EnvironmentId = StagingEnvId,
                ScheduledAt = DateTime.UtcNow.AddDays(-2),
                ScheduledBy = "emily.dev",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Status = Entities.ScheduledMigrationStatus.Completed,
                ExecutedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(5),
                ExecutionSuccess = true,
                Notes = "Multi-tenancy rollout to staging"
            },
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240720_AddCachingLayer",
                EnvironmentId = ProdEnvId,
                ScheduledAt = DateTime.UtcNow.AddDays(-5),
                ScheduledBy = "sarah.dba",
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                Status = Entities.ScheduledMigrationStatus.Completed,
                ExecutedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(2),
                ExecutionSuccess = true,
                Notes = "Caching layer for improved performance"
            },
            // Failed scheduled migration
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240805_FailedMigration",
                EnvironmentId = QaEnvId,
                ScheduledAt = DateTime.UtcNow.AddDays(-3),
                ScheduledBy = "mike.dev",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                Status = Entities.ScheduledMigrationStatus.Failed,
                ExecutedAt = DateTime.UtcNow.AddDays(-3).AddMinutes(1),
                ExecutionSuccess = false,
                ErrorMessage = "Foreign key constraint violation - Orders table references Products",
                Notes = "Attempted product table modification"
            },
            // Cancelled scheduled migration
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240810_CancelledFeature",
                EnvironmentId = DevEnvId,
                ScheduledAt = DateTime.UtcNow.AddDays(-1),
                ScheduledBy = "john.dev",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Status = Entities.ScheduledMigrationStatus.Cancelled,
                CancelledBy = "john.dev",
                CancelledAt = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                CancellationReason = "Feature requirements changed - needs redesign"
            }
        };

        await _context.ScheduledMigrations.AddRangeAsync(scheduled, cancellationToken);
    }

    private async Task SeedApprovalRequestsAsync(CancellationToken cancellationToken)
    {
        var approvals = new List<ApprovalRequestEntity>
        {
            // Pending approval requests
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240815_AddNewFeature",
                EnvironmentId = ProdEnvId,
                EnvironmentName = "Production (US)",
                RequestedBy = "john.dev",
                RequestedByEmail = "john.smith@company.com",
                RequestedAt = DateTime.UtcNow.AddHours(-3),
                Status = "Pending",
                RequestComments = "New customer analytics feature - tested in staging for 1 week",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240816_PerformanceOptimization",
                EnvironmentId = ProdEuEnvId,
                EnvironmentName = "Production (EU)",
                RequestedBy = "emily.dev",
                RequestedByEmail = "emily.chen@company.com",
                RequestedAt = DateTime.UtcNow.AddHours(-5),
                Status = "Pending",
                RequestComments = "Query optimization for EU customers - reduces page load by 40%",
                ExpiresAt = DateTime.UtcNow.AddDays(2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240817_SecurityPatch",
                EnvironmentId = ProdEnvId,
                EnvironmentName = "Production (US)",
                RequestedBy = "sarah.dba",
                RequestedByEmail = "sarah.johnson@company.com",
                RequestedAt = DateTime.UtcNow.AddHours(-1),
                Status = "Pending",
                RequestComments = "URGENT: Security vulnerability patch - CVE-2024-1234",
                ExpiresAt = DateTime.UtcNow.AddHours(12)
            },
            // Approved requests
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240801_AddMultiTenancy",
                EnvironmentId = ProdEnvId,
                EnvironmentName = "Production (US)",
                RequestedBy = "emily.dev",
                RequestedByEmail = "emily.chen@company.com",
                RequestedAt = DateTime.UtcNow.AddDays(-3),
                Status = "Approved",
                RequestComments = "Multi-tenancy support for enterprise customers",
                ReviewedBy = "sarah.dba",
                ReviewedByEmail = "sarah.johnson@company.com",
                ReviewedAt = DateTime.UtcNow.AddDays(-2).AddHours(-1),
                ReviewComments = "Reviewed test results - looks good. Approved for production.",
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240720_AddCachingLayer",
                EnvironmentId = ProdEnvId,
                EnvironmentName = "Production (US)",
                RequestedBy = "john.dev",
                RequestedByEmail = "john.smith@company.com",
                RequestedAt = DateTime.UtcNow.AddDays(-7),
                Status = "Approved",
                RequestComments = "Redis caching implementation for session management",
                ReviewedBy = "sarah.dba",
                ReviewedByEmail = "sarah.johnson@company.com",
                ReviewedAt = DateTime.UtcNow.AddDays(-6),
                ReviewComments = "Performance benchmarks verified. Go ahead.",
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddDays(-5)
            },
            // Rejected request
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240805_FailedMigration",
                EnvironmentId = ProdEnvId,
                EnvironmentName = "Production (US)",
                RequestedBy = "mike.dev",
                RequestedByEmail = "mike.wilson@company.com",
                RequestedAt = DateTime.UtcNow.AddDays(-4),
                Status = "Rejected",
                RequestComments = "Restructure products table for better indexing",
                ReviewedBy = "sarah.dba",
                ReviewedByEmail = "sarah.johnson@company.com",
                ReviewedAt = DateTime.UtcNow.AddDays(-4).AddHours(2),
                RejectionReason = "Migration will break FK constraints with Orders table. Please fix dependency issues first."
            },
            // Expired request
            new()
            {
                Id = Guid.NewGuid(),
                MigrationId = "20240810_CancelledFeature",
                EnvironmentId = ProdEnvId,
                EnvironmentName = "Production (US)",
                RequestedBy = "john.dev",
                RequestedByEmail = "john.smith@company.com",
                RequestedAt = DateTime.UtcNow.AddDays(-10),
                Status = "Expired",
                RequestComments = "New notification system feature",
                ExpiresAt = DateTime.UtcNow.AddDays(-8)
            }
        };

        await _context.ApprovalRequests.AddRangeAsync(approvals, cancellationToken);
    }

    private static AuditAction GetRandomAction()
    {
        var actions = new[]
        {
            AuditAction.AppliedMigration,
            AuditAction.AppliedMigration,
            AuditAction.AppliedMigration,
            AuditAction.RolledBackMigration,
            AuditAction.AddedEnvironment,
            AuditAction.ModifiedEnvironment,
            AuditAction.LoginSuccess,
            AuditAction.LoginSuccess,
            AuditAction.LoginFailed,
            AuditAction.ApprovalRequested,
            AuditAction.ApprovalGranted,
            AuditAction.ApprovalDenied,
            AuditAction.ScheduledMigration,
            AuditAction.CancelledSchedule
        };
        return actions[_random.Next(actions.Length)];
    }

    private static string? GetNotesForAction(AuditAction action)
    {
        return action switch
        {
            AuditAction.AppliedMigration => "Migration applied successfully",
            AuditAction.RolledBackMigration => "Rolled back due to issues",
            AuditAction.LoginSuccess => "User logged in",
            AuditAction.LoginFailed => "Login attempt failed",
            AuditAction.ApprovalRequested => "Approval requested for production deployment",
            AuditAction.ApprovalGranted => "Approved for deployment",
            AuditAction.ApprovalDenied => "Denied - needs additional review",
            _ => null
        };
    }
}
