using MigrationCommander.Core.Models;

namespace MigrationCommander.Core.Exceptions;

/// <summary>
/// Exception thrown when a database provider is not supported.
/// </summary>
public class ProviderNotSupportedException : Exception
{
    /// <summary>
    /// The provider type that is not supported.
    /// </summary>
    public ProviderType Provider { get; }

    public ProviderNotSupportedException(ProviderType provider)
        : base($"Database provider '{provider}' is not supported.")
    {
        Provider = provider;
    }

    public ProviderNotSupportedException(ProviderType provider, string message)
        : base(message)
    {
        Provider = provider;
    }

    public ProviderNotSupportedException(ProviderType provider, string message, Exception innerException)
        : base(message, innerException)
    {
        Provider = provider;
    }
}

/// <summary>
/// Exception thrown when a connection to a database fails.
/// </summary>
public class DatabaseConnectionException : Exception
{
    /// <summary>
    /// The environment ID that failed to connect.
    /// </summary>
    public Guid? EnvironmentId { get; }

    /// <summary>
    /// The provider type.
    /// </summary>
    public ProviderType? Provider { get; }

    public DatabaseConnectionException(string message) : base(message)
    {
    }

    public DatabaseConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DatabaseConnectionException(string message, Guid environmentId, ProviderType provider)
        : base(message)
    {
        EnvironmentId = environmentId;
        Provider = provider;
    }

    public DatabaseConnectionException(string message, Guid environmentId, ProviderType provider, Exception innerException)
        : base(message, innerException)
    {
        EnvironmentId = environmentId;
        Provider = provider;
    }
}

/// <summary>
/// Exception thrown when an environment is not found.
/// </summary>
public class EnvironmentNotFoundException : Exception
{
    /// <summary>
    /// The environment ID that was not found.
    /// </summary>
    public Guid? EnvironmentId { get; }

    /// <summary>
    /// The environment name that was not found (if searched by name).
    /// </summary>
    public string? EnvironmentName { get; }

    public EnvironmentNotFoundException(Guid environmentId)
        : base($"Environment with ID '{environmentId}' was not found.")
    {
        EnvironmentId = environmentId;
    }

    public EnvironmentNotFoundException(string environmentName)
        : base($"Environment with name '{environmentName}' was not found.")
    {
        EnvironmentName = environmentName;
    }
}
