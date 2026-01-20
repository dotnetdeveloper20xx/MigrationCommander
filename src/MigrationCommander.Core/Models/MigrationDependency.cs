namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents dependency relationships between migrations.
/// </summary>
public class MigrationDependency
{
    /// <summary>
    /// The migration ID.
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// Migration IDs that must be applied before this migration.
    /// </summary>
    public List<string> DependsOn { get; set; } = new();

    /// <summary>
    /// Migration IDs that depend on this migration (reverse lookup).
    /// </summary>
    public List<string> Blocks { get; set; } = new();

    /// <summary>
    /// Whether all dependencies are satisfied for a given environment.
    /// </summary>
    public bool DependenciesSatisfied { get; set; }

    /// <summary>
    /// List of unsatisfied dependencies (if any).
    /// </summary>
    public List<string> UnsatisfiedDependencies { get; set; } = new();
}

/// <summary>
/// Result of dependency validation.
/// </summary>
public class DependencyValidationResult
{
    /// <summary>
    /// Whether all dependencies are valid and can be resolved.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Migration execution order (topologically sorted).
    /// </summary>
    public List<string> ExecutionOrder { get; set; } = new();

    /// <summary>
    /// List of circular dependency chains found.
    /// </summary>
    public List<List<string>> CircularDependencies { get; set; } = new();

    /// <summary>
    /// Migrations that have missing dependencies.
    /// </summary>
    public Dictionary<string, List<string>> MissingDependencies { get; set; } = new();

    /// <summary>
    /// Validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a valid result with the given execution order.
    /// </summary>
    public static DependencyValidationResult Valid(List<string> executionOrder)
    {
        return new DependencyValidationResult
        {
            IsValid = true,
            ExecutionOrder = executionOrder
        };
    }

    /// <summary>
    /// Creates an invalid result with the given errors.
    /// </summary>
    public static DependencyValidationResult Invalid(params string[] errors)
    {
        return new DependencyValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}
