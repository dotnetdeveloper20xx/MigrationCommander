using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Services;

/// <summary>
/// Service for discovering migrations from EF Core DbContext assemblies.
/// </summary>
public class MigrationDiscoveryService : IMigrationDiscovery
{
    private readonly IMigrationProviderFactory _providerFactory;
    private readonly Dictionary<Type, List<MigrationInfo>> _migrationCache = new();
    private static readonly Regex MigrationIdPattern = new(@"^(\d{14})_(.+)$", RegexOptions.Compiled);

    public MigrationDiscoveryService(IMigrationProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MigrationInfo>> DiscoverMigrationsAsync(
        Type dbContextType,
        CancellationToken cancellationToken = default)
    {
        if (!typeof(DbContext).IsAssignableFrom(dbContextType))
        {
            throw new ArgumentException($"Type {dbContextType.Name} is not a DbContext", nameof(dbContextType));
        }

        // Check cache first
        if (_migrationCache.TryGetValue(dbContextType, out var cachedMigrations))
        {
            return cachedMigrations;
        }

        var migrations = new List<MigrationInfo>();
        var assembly = dbContextType.Assembly;

        // Find all migration types in the assembly
        var migrationTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Migration).IsAssignableFrom(t))
            .ToList();

        foreach (var migrationType in migrationTypes)
        {
            var migrationId = GetMigrationId(migrationType);
            if (string.IsNullOrEmpty(migrationId))
                continue;

            var migrationInfo = CreateMigrationInfo(migrationType, migrationId);
            migrations.Add(migrationInfo);
        }

        // Sort by timestamp
        migrations = migrations.OrderBy(m => m.Timestamp).ToList();

        // Cache the results
        _migrationCache[dbContextType] = migrations;

        return await Task.FromResult<IReadOnlyList<MigrationInfo>>(migrations);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MigrationInfo>> GetAppliedMigrationsAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetProvider(environment.Provider);
        var appliedMigrationIds = await provider.GetAppliedMigrationsAsync(
            environment.ConnectionString,
            cancellationToken);

        var appliedMigrations = new List<MigrationInfo>();

        foreach (var migrationId in appliedMigrationIds)
        {
            var info = ParseMigrationId(migrationId);
            info.Status = MigrationStatus.Applied;
            appliedMigrations.Add(info);
        }

        return appliedMigrations.OrderBy(m => m.Timestamp).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MigrationInfo>> GetPendingMigrationsAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        // This requires knowing the target DbContext type
        // For now, return based on applied migrations comparison
        var appliedMigrations = await GetAppliedMigrationsAsync(environment, cancellationToken);
        var appliedIds = appliedMigrations.Select(m => m.Id).ToHashSet();

        // Get all discovered migrations from cache (if available)
        var allMigrations = _migrationCache.Values.SelectMany(m => m).ToList();

        var pendingMigrations = allMigrations
            .Where(m => !appliedIds.Contains(m.Id))
            .Select(m =>
            {
                var pending = CloneMigrationInfo(m);
                pending.Status = MigrationStatus.Pending;
                return pending;
            })
            .OrderBy(m => m.Timestamp)
            .ToList();

        return pendingMigrations;
    }

    /// <inheritdoc />
    public async Task<EnvironmentComparisonResult> CompareEnvironmentsAsync(
        IEnumerable<DatabaseEnvironment> environments,
        CancellationToken cancellationToken = default)
    {
        var result = new EnvironmentComparisonResult();
        var environmentList = environments.ToList();
        var allMigrationIds = new HashSet<string>();
        var migrationStatuses = new Dictionary<string, Dictionary<Guid, MigrationStatus>>();

        foreach (var env in environmentList)
        {
            var applied = await GetAppliedMigrationsAsync(env, cancellationToken);
            var appliedIds = applied.Select(m => m.Id).ToHashSet();

            foreach (var id in appliedIds)
            {
                allMigrationIds.Add(id);
            }

            result.Environments.Add(new EnvironmentSummary
            {
                Id = env.Id,
                Name = env.Name,
                DisplayName = env.DisplayName,
                Provider = env.Provider,
                AppliedCount = applied.Count,
                LastMigrationAt = applied.LastOrDefault()?.AppliedAt
            });
        }

        // Also include migrations from cache
        foreach (var migration in _migrationCache.Values.SelectMany(m => m))
        {
            allMigrationIds.Add(migration.Id);
        }

        result.AllMigrations = allMigrationIds.OrderBy(id => id).ToList();

        // Build status matrix
        foreach (var migrationId in result.AllMigrations)
        {
            var statusByEnv = new Dictionary<Guid, MigrationStatus>();

            foreach (var env in environmentList)
            {
                var applied = await GetAppliedMigrationsAsync(env, cancellationToken);
                var isApplied = applied.Any(m => m.Id == migrationId);
                statusByEnv[env.Id] = isApplied ? MigrationStatus.Applied : MigrationStatus.Pending;
            }

            migrationStatuses[migrationId] = statusByEnv;

            // Check for sync issues
            var statuses = statusByEnv.Values.Distinct().ToList();
            if (statuses.Count > 1)
            {
                var affectedEnvs = statusByEnv
                    .Where(kvp => kvp.Value == MigrationStatus.Pending)
                    .Select(kvp => kvp.Key)
                    .ToList();

                result.SyncIssues.Add(new MigrationSyncIssue
                {
                    MigrationId = migrationId,
                    Description = $"Migration is applied in {statusByEnv.Count - affectedEnvs.Count} environment(s) but pending in {affectedEnvs.Count}",
                    AffectedEnvironments = affectedEnvs,
                    IssueType = SyncIssueType.PartiallyApplied
                });
            }
        }

        result.MigrationStatuses = migrationStatuses;

        // Update pending counts
        foreach (var envSummary in result.Environments)
        {
            envSummary.PendingCount = migrationStatuses
                .Count(kvp => kvp.Value.TryGetValue(envSummary.Id, out var status) && status == MigrationStatus.Pending);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<MigrationInfo?> GetMigrationByIdAsync(
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        foreach (var migrations in _migrationCache.Values)
        {
            var migration = migrations.FirstOrDefault(m => m.Id == migrationId);
            if (migration != null)
            {
                return await Task.FromResult(CloneMigrationInfo(migration));
            }
        }

        // If not in cache, try to parse it
        return await Task.FromResult(ParseMigrationId(migrationId));
    }

    /// <summary>
    /// Registers migrations from a DbContext for discovery.
    /// Call this during startup to populate the migration cache.
    /// </summary>
    public async Task RegisterDbContextAsync<TContext>(CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        await DiscoverMigrationsAsync(typeof(TContext), cancellationToken);
    }

    private static string? GetMigrationId(Type migrationType)
    {
        // Try to get the migration ID from the MigrationAttribute
        var attribute = migrationType.GetCustomAttribute<MigrationAttribute>();
        if (attribute != null)
        {
            return attribute.Id;
        }

        // Fall back to parsing the type name
        var typeName = migrationType.Name;
        if (MigrationIdPattern.IsMatch(typeName))
        {
            return typeName;
        }

        return null;
    }

    private static MigrationInfo CreateMigrationInfo(Type migrationType, string migrationId)
    {
        var info = ParseMigrationId(migrationId);
        info.TypeName = migrationType.FullName;
        info.AssemblyName = migrationType.Assembly.GetName().Name;

        // Try to get description from XML documentation or attribute
        var descriptionAttr = migrationType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        if (descriptionAttr != null)
        {
            info.Description = descriptionAttr.Description;
        }

        return info;
    }

    private static MigrationInfo ParseMigrationId(string migrationId)
    {
        var info = new MigrationInfo
        {
            Id = migrationId,
            Status = MigrationStatus.Pending
        };

        var match = MigrationIdPattern.Match(migrationId);
        if (match.Success)
        {
            var timestampStr = match.Groups[1].Value;
            info.Name = match.Groups[2].Value;

            // Parse timestamp: yyyyMMddHHmmss
            if (DateTime.TryParseExact(timestampStr, "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var timestamp))
            {
                info.Timestamp = timestamp;
            }
        }
        else
        {
            info.Name = migrationId;
        }

        return info;
    }

    private static MigrationInfo CloneMigrationInfo(MigrationInfo source)
    {
        return new MigrationInfo
        {
            Id = source.Id,
            Name = source.Name,
            Timestamp = source.Timestamp,
            Description = source.Description,
            Status = source.Status,
            AppliedAt = source.AppliedAt,
            AppliedBy = source.AppliedBy,
            UpSql = source.UpSql,
            DownSql = source.DownSql,
            AffectedTables = new List<string>(source.AffectedTables),
            CreatedObjects = new List<string>(source.CreatedObjects),
            DroppedObjects = new List<string>(source.DroppedObjects),
            ModifiedObjects = new List<string>(source.ModifiedObjects),
            IsDestructive = source.IsDestructive,
            EstimatedDuration = source.EstimatedDuration,
            TypeName = source.TypeName,
            AssemblyName = source.AssemblyName
        };
    }
}
