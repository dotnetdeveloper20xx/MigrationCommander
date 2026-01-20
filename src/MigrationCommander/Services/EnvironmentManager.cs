using Microsoft.AspNetCore.DataProtection;
using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Data.Entities;
using MigrationCommander.Data.Repositories;

namespace MigrationCommander.Services;

/// <summary>
/// Service for managing database environments.
/// </summary>
public class EnvironmentManager : IEnvironmentManager
{
    private readonly DatabaseRepository _databaseRepository;
    private readonly IMigrationProviderFactory _providerFactory;
    private readonly IMigrationDiscovery _migrationDiscovery;
    private readonly IDataProtector _protector;

    public EnvironmentManager(
        DatabaseRepository databaseRepository,
        IMigrationProviderFactory providerFactory,
        IMigrationDiscovery migrationDiscovery,
        IDataProtectionProvider dataProtectionProvider)
    {
        _databaseRepository = databaseRepository;
        _providerFactory = providerFactory;
        _migrationDiscovery = migrationDiscovery;
        _protector = dataProtectionProvider.CreateProtector("MigrationCommander.ConnectionStrings");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DatabaseEnvironment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var databases = await _databaseRepository.GetAllAsync(cancellationToken);
        return databases.Select(db => db.ToDomainModel(DecryptConnectionString(db.ConnectionStringEncrypted))).ToList();
    }

    /// <inheritdoc />
    public async Task<DatabaseEnvironment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var database = await _databaseRepository.GetByIdAsync(id, cancellationToken);
        return database?.ToDomainModel(DecryptConnectionString(database.ConnectionStringEncrypted));
    }

    /// <inheritdoc />
    public async Task<DatabaseEnvironment?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var database = await _databaseRepository.GetByNameAsync(name, cancellationToken);
        return database?.ToDomainModel(DecryptConnectionString(database.ConnectionStringEncrypted));
    }

    /// <inheritdoc />
    public async Task<DatabaseEnvironment> AddAsync(DatabaseEnvironment environment, CancellationToken cancellationToken = default)
    {
        // Validate connection before adding
        var (success, errorMessage) = await TestConnectionAsync(environment, cancellationToken);
        if (!success)
        {
            throw new DatabaseConnectionException(
                $"Cannot add environment: {errorMessage}",
                environment.Id,
                environment.Provider);
        }

        // Check for duplicate name
        if (await _databaseRepository.ExistsAsync(environment.Name, cancellationToken))
        {
            throw new InvalidOperationException($"An environment with name '{environment.Name}' already exists.");
        }

        var encryptedConnectionString = EncryptConnectionString(environment.ConnectionString);
        var entity = ConfiguredDatabase.FromDomainModel(environment, encryptedConnectionString);

        await _databaseRepository.AddAsync(entity, cancellationToken);

        return entity.ToDomainModel(environment.ConnectionString);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(DatabaseEnvironment environment, CancellationToken cancellationToken = default)
    {
        var existing = await _databaseRepository.GetByIdAsync(environment.Id, cancellationToken);
        if (existing == null)
        {
            throw new EnvironmentNotFoundException(environment.Id);
        }

        // If connection string changed, validate it
        var existingConnectionString = DecryptConnectionString(existing.ConnectionStringEncrypted);
        if (environment.ConnectionString != existingConnectionString)
        {
            var (success, errorMessage) = await TestConnectionAsync(environment, cancellationToken);
            if (!success)
            {
                throw new DatabaseConnectionException(
                    $"Cannot update environment: {errorMessage}",
                    environment.Id,
                    environment.Provider);
            }
        }

        existing.Name = environment.Name;
        existing.DisplayName = environment.DisplayName;
        existing.Provider = environment.Provider;
        existing.ConnectionStringEncrypted = EncryptConnectionString(environment.ConnectionString);
        existing.IsProduction = environment.IsProduction;
        existing.RequiresApproval = environment.RequiresApproval;
        existing.IsActive = environment.IsActive;
        existing.Color = environment.Color;
        existing.SortOrder = environment.SortOrder;

        await _databaseRepository.UpdateAsync(existing, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _databaseRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new EnvironmentNotFoundException(id);
        }

        await _databaseRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(
        DatabaseEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = _providerFactory.GetProvider(environment.Provider);
            return await provider.TestConnectionAsync(environment.ConnectionString, cancellationToken);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task RefreshStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var environment = await GetByIdAsync(id, cancellationToken);
        if (environment == null)
        {
            throw new EnvironmentNotFoundException(id);
        }

        try
        {
            // Get applied migrations
            var applied = await _migrationDiscovery.GetAppliedMigrationsAsync(environment, cancellationToken);
            var pending = await _migrationDiscovery.GetPendingMigrationsAsync(environment, cancellationToken);

            environment.AppliedMigrationCount = applied.Count;
            environment.PendingMigrationCount = pending.Count;
            environment.LastCheckedAt = DateTime.UtcNow;
            environment.LastError = null;

            if (applied.Any())
            {
                var lastApplied = applied.Last();
                environment.LastMigrationAt = lastApplied.AppliedAt;
            }

            await UpdateAsync(environment, cancellationToken);
        }
        catch (Exception ex)
        {
            environment.LastError = ex.Message;
            environment.LastCheckedAt = DateTime.UtcNow;
            await UpdateAsync(environment, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DatabaseEnvironment>> GetByProviderAsync(
        ProviderType provider,
        CancellationToken cancellationToken = default)
    {
        var databases = await _databaseRepository.GetByProviderAsync(provider, cancellationToken);
        return databases.Select(db => db.ToDomainModel(DecryptConnectionString(db.ConnectionStringEncrypted))).ToList();
    }

    private string EncryptConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return string.Empty;

        return _protector.Protect(connectionString);
    }

    private string DecryptConnectionString(string encryptedConnectionString)
    {
        if (string.IsNullOrEmpty(encryptedConnectionString))
            return string.Empty;

        try
        {
            return _protector.Unprotect(encryptedConnectionString);
        }
        catch
        {
            // If decryption fails, return empty (keys may have changed)
            return string.Empty;
        }
    }
}
