using MigrationCommander.Core.Models;

namespace MigrationCommander.Data.Entities;

/// <summary>
/// Entity representing a configured database environment.
/// </summary>
public class ConfiguredDatabase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ProviderType Provider { get; set; }

    /// <summary>
    /// Encrypted connection string.
    /// </summary>
    public string ConnectionStringEncrypted { get; set; } = string.Empty;

    public bool IsProduction { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; } = true;

    public string? Color { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Converts to the domain model.
    /// </summary>
    public DatabaseEnvironment ToDomainModel(string decryptedConnectionString)
    {
        return new DatabaseEnvironment
        {
            Id = Id,
            Name = Name,
            DisplayName = DisplayName,
            Provider = Provider,
            ConnectionString = decryptedConnectionString,
            IsProduction = IsProduction,
            RequiresApproval = RequiresApproval,
            IsActive = IsActive,
            Color = Color,
            SortOrder = SortOrder,
            CreatedAt = CreatedAt
        };
    }

    /// <summary>
    /// Creates from a domain model.
    /// </summary>
    public static ConfiguredDatabase FromDomainModel(DatabaseEnvironment environment, string encryptedConnectionString)
    {
        return new ConfiguredDatabase
        {
            Id = environment.Id == Guid.Empty ? Guid.NewGuid() : environment.Id,
            Name = environment.Name,
            DisplayName = environment.DisplayName,
            Provider = environment.Provider,
            ConnectionStringEncrypted = encryptedConnectionString,
            IsProduction = environment.IsProduction,
            RequiresApproval = environment.RequiresApproval,
            IsActive = environment.IsActive,
            Color = environment.Color,
            SortOrder = environment.SortOrder,
            CreatedAt = environment.CreatedAt == default ? DateTime.UtcNow : environment.CreatedAt
        };
    }
}
