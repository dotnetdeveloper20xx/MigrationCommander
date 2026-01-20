using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for resolving migration dependencies and determining execution order.
/// </summary>
public class DependencyResolverService : IDependencyResolver
{
    private readonly HistoryRepository _historyRepository;

    // In-memory storage for dependencies (in production, this would be persisted)
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();
    private readonly object _lock = new();

    public DependencyResolverService(HistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    /// <inheritdoc />
    public Task<MigrationDependency> GetDependenciesAsync(
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var dependency = new MigrationDependency
        {
            MigrationId = migrationId,
            DependsOn = new List<string>(),
            Blocks = new List<string>()
        };

        lock (_lock)
        {
            // Get what this migration depends on
            if (_dependencies.TryGetValue(migrationId, out var deps))
            {
                dependency.DependsOn = deps.ToList();
            }

            // Get what depends on this migration (reverse lookup)
            foreach (var kvp in _dependencies)
            {
                if (kvp.Value.Contains(migrationId))
                {
                    dependency.Blocks.Add(kvp.Key);
                }
            }
        }

        return Task.FromResult(dependency);
    }

    /// <inheritdoc />
    public Task<DependencyValidationResult> GetExecutionOrderAsync(
        IEnumerable<string> migrationIds,
        CancellationToken cancellationToken = default)
    {
        var idList = migrationIds.ToList();

        if (!idList.Any())
        {
            return Task.FromResult(DependencyValidationResult.Valid(new List<string>()));
        }

        // Build a graph of dependencies for the requested migrations
        var graph = new Dictionary<string, HashSet<string>>();
        var inDegree = new Dictionary<string, int>();

        // Initialize
        foreach (var id in idList)
        {
            graph[id] = new HashSet<string>();
            inDegree[id] = 0;
        }

        // Build edges
        lock (_lock)
        {
            foreach (var id in idList)
            {
                if (_dependencies.TryGetValue(id, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        // Only consider dependencies that are in our set
                        if (idList.Contains(dep))
                        {
                            graph[dep].Add(id); // dep -> id (dep must run before id)
                            inDegree[id]++;
                        }
                    }
                }
            }
        }

        // Kahn's algorithm for topological sort
        var queue = new Queue<string>();
        var result = new List<string>();

        // Find all nodes with no dependencies
        foreach (var id in idList)
        {
            if (inDegree[id] == 0)
            {
                queue.Enqueue(id);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var neighbor in graph[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Check for circular dependencies
        if (result.Count != idList.Count)
        {
            var remaining = idList.Where(id => !result.Contains(id)).ToList();
            var circularChains = FindCircularDependencies(remaining);

            var validationResult = new DependencyValidationResult
            {
                IsValid = false,
                ExecutionOrder = result,
                CircularDependencies = circularChains,
                Errors = new List<string>
                {
                    $"Circular dependencies detected involving migrations: {string.Join(", ", remaining)}"
                }
            };

            return Task.FromResult(validationResult);
        }

        return Task.FromResult(DependencyValidationResult.Valid(result));
    }

    /// <inheritdoc />
    public async Task<bool> ValidateDependenciesAsync(
        string migrationId,
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        var dependency = await GetDependenciesAsync(migrationId, cancellationToken);

        if (!dependency.DependsOn.Any())
        {
            return true; // No dependencies to validate
        }

        // Check if all dependencies are applied in the environment
        var histories = await _historyRepository.GetByEnvironmentAsync(environmentId, cancellationToken);
        var appliedMigrations = histories
            .Where(h => h.Status == MigrationStatus.Applied)
            .Select(h => h.MigrationId)
            .ToHashSet();

        foreach (var dep in dependency.DependsOn)
        {
            if (!appliedMigrations.Contains(dep))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetDependentMigrationsAsync(
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var dependents = new List<string>();

        lock (_lock)
        {
            foreach (var kvp in _dependencies)
            {
                if (kvp.Value.Contains(migrationId))
                {
                    dependents.Add(kvp.Key);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(dependents);
    }

    /// <inheritdoc />
    public Task AddDependencyAsync(
        string migrationId,
        string dependsOnMigrationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentException("Migration ID is required", nameof(migrationId));

        if (string.IsNullOrWhiteSpace(dependsOnMigrationId))
            throw new ArgumentException("Depends on migration ID is required", nameof(dependsOnMigrationId));

        if (migrationId == dependsOnMigrationId)
            throw new ArgumentException("A migration cannot depend on itself");

        lock (_lock)
        {
            if (!_dependencies.TryGetValue(migrationId, out var deps))
            {
                deps = new HashSet<string>();
                _dependencies[migrationId] = deps;
            }

            // Check for circular dependency before adding
            if (WouldCreateCircle(migrationId, dependsOnMigrationId))
            {
                throw new InvalidOperationException(
                    $"Adding dependency from '{migrationId}' to '{dependsOnMigrationId}' would create a circular dependency");
            }

            deps.Add(dependsOnMigrationId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveDependencyAsync(
        string migrationId,
        string dependsOnMigrationId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_dependencies.TryGetValue(migrationId, out var deps))
            {
                deps.Remove(dependsOnMigrationId);

                if (!deps.Any())
                {
                    _dependencies.Remove(migrationId);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if adding a dependency would create a circular reference.
    /// </summary>
    private bool WouldCreateCircle(string migrationId, string dependsOnMigrationId)
    {
        // Check if dependsOnMigrationId transitively depends on migrationId
        var visited = new HashSet<string>();
        var toCheck = new Queue<string>();
        toCheck.Enqueue(dependsOnMigrationId);

        while (toCheck.Count > 0)
        {
            var current = toCheck.Dequeue();

            if (current == migrationId)
            {
                return true; // Found a path back to the original migration
            }

            if (visited.Contains(current))
            {
                continue;
            }

            visited.Add(current);

            if (_dependencies.TryGetValue(current, out var deps))
            {
                foreach (var dep in deps)
                {
                    toCheck.Enqueue(dep);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Finds circular dependency chains.
    /// </summary>
    private List<List<string>> FindCircularDependencies(List<string> migrationIds)
    {
        var chains = new List<List<string>>();
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var id in migrationIds)
        {
            if (!visited.Contains(id))
            {
                var chain = new List<string>();
                if (DetectCycle(id, visited, recursionStack, chain))
                {
                    chains.Add(chain);
                }
            }
        }

        return chains;
    }

    /// <summary>
    /// Detects a cycle using DFS.
    /// </summary>
    private bool DetectCycle(
        string migrationId,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> chain)
    {
        visited.Add(migrationId);
        recursionStack.Add(migrationId);
        chain.Add(migrationId);

        if (_dependencies.TryGetValue(migrationId, out var deps))
        {
            foreach (var dep in deps)
            {
                if (!visited.Contains(dep))
                {
                    if (DetectCycle(dep, visited, recursionStack, chain))
                    {
                        return true;
                    }
                }
                else if (recursionStack.Contains(dep))
                {
                    chain.Add(dep); // Add the node that completes the cycle
                    return true;
                }
            }
        }

        recursionStack.Remove(migrationId);
        chain.RemoveAt(chain.Count - 1);
        return false;
    }
}
