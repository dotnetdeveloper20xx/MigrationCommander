namespace MigrationCommander.Core.Exceptions;

/// <summary>
/// Base exception for migration-related errors.
/// </summary>
public class MigrationException : Exception
{
    /// <summary>
    /// The migration ID that caused the error (if applicable).
    /// </summary>
    public string? MigrationId { get; }

    /// <summary>
    /// The environment ID where the error occurred (if applicable).
    /// </summary>
    public Guid? EnvironmentId { get; }

    public MigrationException(string message) : base(message)
    {
    }

    public MigrationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public MigrationException(string message, string migrationId) : base(message)
    {
        MigrationId = migrationId;
    }

    public MigrationException(string message, string migrationId, Guid environmentId) : base(message)
    {
        MigrationId = migrationId;
        EnvironmentId = environmentId;
    }

    public MigrationException(string message, string migrationId, Guid environmentId, Exception innerException)
        : base(message, innerException)
    {
        MigrationId = migrationId;
        EnvironmentId = environmentId;
    }
}

/// <summary>
/// Exception thrown when a migration is not found.
/// </summary>
public class MigrationNotFoundException : MigrationException
{
    public MigrationNotFoundException(string migrationId)
        : base($"Migration '{migrationId}' was not found.", migrationId)
    {
    }
}

/// <summary>
/// Exception thrown when a migration has already been applied.
/// </summary>
public class MigrationAlreadyAppliedException : MigrationException
{
    public MigrationAlreadyAppliedException(string migrationId, Guid environmentId)
        : base($"Migration '{migrationId}' has already been applied to environment '{environmentId}'.", migrationId, environmentId)
    {
    }
}

/// <summary>
/// Exception thrown when a migration execution fails.
/// </summary>
public class MigrationExecutionException : MigrationException
{
    /// <summary>
    /// The SQL that was being executed when the error occurred.
    /// </summary>
    public string? ExecutedSql { get; }

    public MigrationExecutionException(string message, string migrationId, Guid environmentId, string? executedSql = null)
        : base(message, migrationId, environmentId)
    {
        ExecutedSql = executedSql;
    }

    public MigrationExecutionException(string message, string migrationId, Guid environmentId, Exception innerException, string? executedSql = null)
        : base(message, migrationId, environmentId, innerException)
    {
        ExecutedSql = executedSql;
    }
}

/// <summary>
/// Exception thrown when a migration is cancelled.
/// </summary>
public class MigrationCancelledException : MigrationException
{
    public MigrationCancelledException(string migrationId)
        : base($"Migration '{migrationId}' was cancelled.", migrationId)
    {
    }

    public MigrationCancelledException(string migrationId, string reason)
        : base($"Migration '{migrationId}' was cancelled: {reason}", migrationId)
    {
    }
}
