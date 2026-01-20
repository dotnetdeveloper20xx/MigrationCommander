using Microsoft.EntityFrameworkCore;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;

namespace MigrationCommander.Data;

/// <summary>
/// DbContext for MigrationCommander's internal storage.
/// Uses SQLite for storing configuration, audit logs, and history.
/// </summary>
public class MigrationCommanderDbContext : DbContext
{
    public DbSet<ConfiguredDatabase> ConfiguredDatabases => Set<ConfiguredDatabase>();
    public DbSet<MigrationHistory> MigrationHistories => Set<MigrationHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ScheduledMigration> ScheduledMigrations => Set<ScheduledMigration>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<ApprovalRequestEntity> ApprovalRequests => Set<ApprovalRequestEntity>();

    public MigrationCommanderDbContext(DbContextOptions<MigrationCommanderDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureConfiguredDatabase(modelBuilder);
        ConfigureMigrationHistory(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureScheduledMigration(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureUserRole(modelBuilder);
        ConfigureApprovalRequest(modelBuilder);
    }

    private static void ConfigureConfiguredDatabase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfiguredDatabase>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ConnectionStringEncrypted)
                .IsRequired();

            entity.Property(e => e.Provider)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Color)
                .HasMaxLength(20);

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureMigrationHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MigrationHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MigrationId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Provider)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.ExecutedBy)
                .HasMaxLength(200);

            entity.HasOne(e => e.Environment)
                .WithMany()
                .HasForeignKey(e => e.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MigrationId);
            entity.HasIndex(e => e.EnvironmentId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => new { e.EnvironmentId, e.MigrationId });
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.UserEmail)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.UserIpAddress)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Action)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.MigrationId)
                .HasMaxLength(200);

            entity.Property(e => e.EnvironmentName)
                .HasMaxLength(200);

            entity.Property(e => e.Provider)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EnvironmentId);
            entity.HasIndex(e => e.Success);
        });
    }

    private static void ConfigureScheduledMigration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduledMigration>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MigrationId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ScheduledBy)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.CancelledBy)
                .HasMaxLength(200);

            entity.HasOne(e => e.Environment)
                .WithMany()
                .HasForeignKey(e => e.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EnvironmentId);
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ExternalId)
                .HasMaxLength(200);

            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.PermissionsJson)
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });
    }

    private static void ConfigureUserRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRoleEntity>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.AssignedBy)
                .HasMaxLength(200);

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RoleId);
        });
    }

    private static void ConfigureApprovalRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalRequestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MigrationId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.EnvironmentName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.RequestedBy)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.RequestedByEmail)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ReviewedBy)
                .HasMaxLength(200);

            entity.Property(e => e.ReviewedByEmail)
                .HasMaxLength(200);

            entity.Property(e => e.RequestComments)
                .HasMaxLength(2000);

            entity.Property(e => e.ReviewComments)
                .HasMaxLength(2000);

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(2000);

            entity.HasOne(e => e.Environment)
                .WithMany()
                .HasForeignKey(e => e.EnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MigrationId);
            entity.HasIndex(e => e.EnvironmentId);
            entity.HasIndex(e => e.RequestedBy);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);
            entity.HasIndex(e => new { e.EnvironmentId, e.MigrationId, e.Status });
        });
    }
}
