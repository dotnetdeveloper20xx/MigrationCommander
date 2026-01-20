using MigrationCommander.Core.Exceptions;
using MigrationCommander.Core.Interfaces;
using MigrationCommander.Core.Models;
using MigrationCommander.Providers.MySQL;
using MigrationCommander.Providers.PostgreSQL;
using MigrationCommander.Providers.SQLite;
using MigrationCommander.Providers.SqlServer;

namespace MigrationCommander.Providers;

/// <summary>
/// Factory for creating database provider instances.
/// </summary>
public class MigrationProviderFactory : IMigrationProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ProviderType, Func<IMigrationProvider>> _providerFactories;

    public MigrationProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _providerFactories = new Dictionary<ProviderType, Func<IMigrationProvider>>
        {
            { ProviderType.SqlServer, () => new SqlServerMigrationProvider() },
            { ProviderType.PostgreSQL, () => new PostgreSqlMigrationProvider() },
            { ProviderType.MySQL, () => new MySqlMigrationProvider() },
            { ProviderType.SQLite, () => new SqliteMigrationProvider() }
        };
    }

    /// <inheritdoc />
    public IMigrationProvider GetProvider(ProviderType providerType)
    {
        if (_providerFactories.TryGetValue(providerType, out var factory))
        {
            return factory();
        }

        throw new ProviderNotSupportedException(providerType);
    }

    /// <inheritdoc />
    public IReadOnlyList<ProviderType> GetAvailableProviders()
    {
        return _providerFactories.Keys.ToList();
    }
}
