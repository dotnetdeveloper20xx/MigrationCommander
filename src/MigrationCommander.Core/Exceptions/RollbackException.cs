namespace MigrationCommander.Core.Exceptions;

/// <summary>
/// Exception thrown when a rollback operation fails.
/// </summary>
public class RollbackException : MigrationException
{
    /// <summary>
    /// The reason the rollback cannot be performed (if blocked).
    /// </summary>
    public string? BlockingReason { get; }

    public RollbackException(string message, string migrationId)
        : base(message, migrationId)
    {
    }

    public RollbackException(string message, string migrationId, Guid environmentId)
        : base(message, migrationId, environmentId)
    {
    }

    public RollbackException(string message, string migrationId, Guid environmentId, Exception innerException)
        : base(message, migrationId, environmentId, innerException)
    {
    }

    public RollbackException(string message, string migrationId, Guid environmentId, string blockingReason)
        : base(message, migrationId, environmentId)
    {
        BlockingReason = blockingReason;
    }
}

/// <summary>
/// Exception thrown when a rollback is not possible.
/// </summary>
public class RollbackNotPossibleException : RollbackException
{
    public RollbackNotPossibleException(string migrationId, string reason)
        : base($"Cannot rollback migration '{migrationId}': {reason}", migrationId)
    {
    }

    public RollbackNotPossibleException(string migrationId, Guid environmentId, string reason)
        : base($"Cannot rollback migration '{migrationId}' in environment '{environmentId}': {reason}", migrationId, environmentId, reason)
    {
    }
}

/// <summary>
/// Exception thrown when attempting to rollback a migration that has not been applied.
/// </summary>
public class MigrationNotAppliedException : RollbackException
{
    public MigrationNotAppliedException(string migrationId, Guid environmentId)
        : base($"Migration '{migrationId}' has not been applied to environment '{environmentId}'.", migrationId, environmentId)
    {
    }
}

/// <summary>
/// Exception thrown when a rollback has dependent migrations that must be rolled back first.
/// </summary>
public class DependentMigrationsExistException : RollbackException
{
    /// <summary>
    /// The migrations that depend on the migration being rolled back.
    /// </summary>
    public IReadOnlyList<string> DependentMigrations { get; }

    public DependentMigrationsExistException(string migrationId, Guid environmentId, IReadOnlyList<string> dependentMigrations)
        : base($"Migration '{migrationId}' cannot be rolled back because {dependentMigrations.Count} dependent migration(s) exist.", migrationId, environmentId)
    {
        DependentMigrations = dependentMigrations;
    }
}
